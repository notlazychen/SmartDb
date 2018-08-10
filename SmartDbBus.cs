using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace SmartDb
{
    static class SmartDbBus
    {
        public static ConcurrentQueue<string> SqlQueue { get; } = new ConcurrentQueue<string>();

        private static readonly Dictionary<Type, DbTypeInfo> _dbTypes = new Dictionary<Type, DbTypeInfo>();

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
            var type = obj.GetType();
            if (!_dbTypes.TryGetValue(type, out var dbt))
            {
                dbt = new DbTypeInfo(type);
                _dbTypes.Add(type, dbt);
            }
            var kvs = dbt.GetKeyValues(obj);
            string[] @params = new string[kvs.Length + 2];
            kvs.CopyTo(@params, 0);
            @params[kvs.Length] = propertyName;
            @params[kvs.Length + 1] = value.ToString();
            string sql = string.Format(dbt.UpdateSqlFormat, @params);
            return sql;
        }

        private static string BuildInsertSql<T>(T item)
        {
            var type = item.GetType();
            if (!_dbTypes.TryGetValue(type, out var dbt))
            {
                dbt = new DbTypeInfo(type);
                _dbTypes.Add(type, dbt);
            }
            string sql = string.Format(dbt.InsertSqlFormat, dbt.GetAllValues(item));
            return sql;
        }

        private static string BuildDeleteSql<T>(T item)
        {
            var type = item.GetType();
            if (!_dbTypes.TryGetValue(type, out var dbt))
            {
                dbt = new DbTypeInfo(type);
                _dbTypes.Add(type, dbt);
            }
            string sql = string.Format(dbt.DeleteSqlFormat, dbt.GetKeyValues(item));
            return sql;
        }
    }
}
