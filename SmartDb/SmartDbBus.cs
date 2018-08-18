using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace SmartDb
{
    static class SmartDbBus
    {
        public static ConcurrentQueue<string> SqlQueue { get; } = new ConcurrentQueue<string>();

        private static readonly ConcurrentDictionary<Type, DbTypeInfo> _dbTypes = new ConcurrentDictionary<Type, DbTypeInfo>();

        private static DbTypeInfo QueryDbTypeInfo(Object obj)
        {
            var type = obj.GetType();
            var dbt = _dbTypes.GetOrAdd(type, (t) => new DbTypeInfo(type));
            return dbt;
        }
        private static DbTypeInfo QueryDbTypeInfo(Type type)
        {
            var dbt = _dbTypes.GetOrAdd(type, (t) => new DbTypeInfo(type));
            return dbt;
        }

        public static void PreLoad(string assemblyName)
        {
            var assembly = Assembly.Load(assemblyName);

            foreach (var type in assembly.ExportedTypes)
            {
                if (type.GetCustomAttribute<DataContractAttribute>() != null)
                {
                    var t = SmartDbEntityAgentFactory.OfType(type);
                    var dbt = QueryDbTypeInfo(t);
                }
            }
        }

        public static void PlanToWriteToDb<T>(T item, DbActionType type)
        {
            string sql = null;
            switch (type)
            {
                case DbActionType.Insert:
                    sql = BuildInsertSql(item);
                    break;
                case DbActionType.Delete:
                    sql = BuildDeleteSql(item);
                    break;
            }

            if(sql != null)
            {
                SqlQueue.Enqueue(sql);
            }
        }

        public static void PlanToSaveUpdateToDb(object obj, string propertyName, object value)
        {

            string sql = BuildUpdateSql(obj, propertyName, value);

            if (sql != null)
            {
                SqlQueue.Enqueue(sql);
            }
        }

        private static string BuildUpdateSql(object obj, string propertyName, object value)
        {
            var dbt = QueryDbTypeInfo(obj);

            var kvs = dbt.GetKeyValues(obj);
            string[] @params = new string[kvs.Length + 2];
            kvs.CopyTo(@params, 0);
            @params[kvs.Length] = propertyName;
            //if (value.GetType().BaseType == typeof(Enum))
            //{
            //    Enum e = (Enum)value;
            //    @params[kvs.Length + 1] = e.ToString("D");
            //}
            //else if(value.GetType() == typeof(bool))
            //{
            //    bool e = (bool)value;
            //    @params[kvs.Length + 1] = e ? "1":"0";
            //}
            //else if (value.GetType() == typeof(DateTime))
            //{
            //    DateTime e = (DateTime)value;
            //    @params[kvs.Length + 1] = $"'{e.ToString("yyyy-MM-dd HH:mm:ss")}'";
            //}
            //else
            //{
            //    var val = value == null ? "null" : $"'{value.ToString().Replace("'", "\'") }'";
            //    @params[kvs.Length + 1] = val;
            //}
            @params[kvs.Length + 1] = DbTypeInfo.ConvertToString(value);
            string sql = string.Format(dbt.UpdateSqlFormat, @params);
            return sql;
        }

        private static string BuildInsertSql<T>(T item)
        {
            var dbt = QueryDbTypeInfo(item);
            string sql = string.Format(dbt.InsertSqlFormat, dbt.GetAllValues(item));
            return sql;
        }

        private static string BuildDeleteSql<T>(T item)
        {
            var dbt = QueryDbTypeInfo(item);
            string sql = string.Format(dbt.DeleteSqlFormat, dbt.GetKeyValues(item));
            return sql;
        }
    }
}
