using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SmartDb
{

    public class DbTypeInfo
    {
        //private static Dictionary<Type, MySqlDbType> _DbTypeMap = new Dictionary<Type, MySqlDbType>();
        //static DbTypeInfo()
        //{
        //    _DbTypeMap.Add(typeof(short), MySqlDbType.Int16);
        //    _DbTypeMap.Add(typeof(int), MySqlDbType.Int32);
        //    _DbTypeMap.Add(typeof(long), MySqlDbType.Int64);
        //    _DbTypeMap.Add(typeof(ushort), MySqlDbType.UInt16);
        //    _DbTypeMap.Add(typeof(uint), MySqlDbType.UInt32);
        //    _DbTypeMap.Add(typeof(ulong), MySqlDbType.UInt64);
        //    _DbTypeMap.Add(typeof(string), MySqlDbType.VarString);
        //    _DbTypeMap.Add(typeof(bool), MySqlDbType.Bit);
        //    _DbTypeMap.Add(typeof(float), MySqlDbType.Float);
        //    _DbTypeMap.Add(typeof(double), MySqlDbType.Double);
        //    _DbTypeMap.Add(typeof(DateTime), MySqlDbType.DateTime);
        //    _DbTypeMap.Add(typeof(TimeSpan), MySqlDbType.Time);

        //    //_DbTypeMap.Add(typeof(short?), MySqlDbType.);
        //    //_DbTypeMap.Add(typeof(int?), MySqlDbType.Int32);
        //    //_DbTypeMap.Add(typeof(long?), MySqlDbType.Int64);
        //    //_DbTypeMap.Add(typeof(bool?), MySqlDbType.Bit);
        //    //_DbTypeMap.Add(typeof(float?), MySqlDbType.Float);
        //    //_DbTypeMap.Add(typeof(double?), MySqlDbType.Double);
        //    //_DbTypeMap.Add(typeof(DateTime?), MySqlDbType.DateTime);
        //    //_DbTypeMap.Add(typeof(TimeSpan?), MySqlDbType.Time);
        //    //_DbTypeMap.Add(typeof(ushort?), MySqlDbType.UInt16);
        //    //_DbTypeMap.Add(typeof(uint?), MySqlDbType.UInt32);
        //    //_DbTypeMap.Add(typeof(ulong?), MySqlDbType.UInt64);
        //}

        public Dictionary<string, DbTypePropertyInfo> DbTypeProperties { get; }

        public string InsertSqlFormat { get; }
        public string DeleteSqlFormat { get; }
        public string UpdateSqlFormat { get; }        

        public DbTypeInfo(Type type)
        {
            if(type.BaseType != null && type.Name.EndsWith("Proxy"))
            {
                type = type.BaseType;
            }
            else
            {
                throw new Exception("被托管的实体类型必须是代理类型!");
            }
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            DbTypeProperties = properties.Select(p => new DbTypePropertyInfo
            {
                PropertyInfo = p,
                IsKey = p.GetCustomAttribute<KeyAttribute>() != null
                //ParameterName = $"@{p.Name}",
                //SqlDbType = _DbTypeMap[p.PropertyType],
            }).ToDictionary(p => p.PropertyInfo.Name, p => p);

            var propnames = properties.Select(p => p.Name);
            string sqlprop = string.Join(",", propnames.Select(p => $"`{p}`"));
            string sqlpropValue = string.Join(",", propnames.Select((p, i) => $"'{{{i}}}'"));
            InsertSqlFormat = $"insert into {type.Name}({sqlprop}) values({sqlpropValue});";
            var keys = DbTypeProperties.Values.Where(p => p.IsKey).ToList();
            if (keys.Count == 0)
            {
                throw new SmartDbDomainReflectException(type, $"缺少主键");
            }
            string whereKeys = string.Join(" and ", keys.Select((p, i) => $"`{p.PropertyInfo.Name}`='{{{i}}}'"));
            DeleteSqlFormat = $"delete from {type.Name} where {whereKeys}";
            UpdateSqlFormat = $"update {type.Name} set `{{{keys.Count}}}`='{{{keys.Count+1}}}' where {whereKeys}";
        }

        /// <summary>
        /// 得到所有属性的值
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public string[] GetAllValues(object item)
        {
            return DbTypeProperties.Values.Select((p, i) => p.PropertyInfo.GetValue(item).ToString()).ToArray();
        }

        /// <summary>
        /// 得到组成主键的属性的值
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public string[] GetKeyValues(object item)
        {
            return DbTypeProperties.Values.Where(p=>p.IsKey).Select((p, i) => p.PropertyInfo.GetValue(item).ToString()).ToArray();
        }
    }
}
