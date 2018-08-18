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
        private static Dictionary<Type, Func<object, string>> _DbTypeMap = new Dictionary<Type, Func<object, string>>();
        static DbTypeInfo()
        {
            _DbTypeMap.Add(typeof(short), o=>o.ToString());
            _DbTypeMap.Add(typeof(int), o => o.ToString());
            _DbTypeMap.Add(typeof(long), o => o.ToString());
            _DbTypeMap.Add(typeof(ushort), o => o.ToString());
            _DbTypeMap.Add(typeof(uint), o => o.ToString());
            _DbTypeMap.Add(typeof(ulong), o => o.ToString());
            _DbTypeMap.Add(typeof(string), o => string.Format("'{0}'", o.ToString().Replace("'", "\'")));
            _DbTypeMap.Add(typeof(bool), o => (bool)o ? "1" : "0");
            _DbTypeMap.Add(typeof(float), o => o.ToString());
            _DbTypeMap.Add(typeof(double), o => o.ToString());
            _DbTypeMap.Add(typeof(DateTime), o => string.Format("'{0}'", ((DateTime)o).ToString("yyyy-MM-dd HH:mm:ss")));
            //_DbTypeMap.Add(typeof(TimeSpan), o => string.Format("'{0}'", ((TimeSpan)o).ToString("HH:mm:ss")));

            _DbTypeMap.Add(typeof(Enum), o => o.ToString());

            _DbTypeMap.Add(typeof(short?), o => o == null ? "null" : o.ToString());
            _DbTypeMap.Add(typeof(int?), o => o == null ? "null" : o.ToString());
            _DbTypeMap.Add(typeof(long?), o => o == null ? "null" : o.ToString());
            _DbTypeMap.Add(typeof(ushort?), o => o == null ? "null" : o.ToString());
            _DbTypeMap.Add(typeof(uint?), o => o == null ? "null" : o.ToString());
            _DbTypeMap.Add(typeof(ulong?), o => o == null ? "null" : o.ToString());
            _DbTypeMap.Add(typeof(bool?), o => o == null ? "null" : (bool)o ? "1" : "0");
            _DbTypeMap.Add(typeof(float?), o => o == null ? "null" : o.ToString());
            _DbTypeMap.Add(typeof(double?), o => o == null ? "null" : o.ToString());
            _DbTypeMap.Add(typeof(DateTime?), o => o == null ? "null": string.Format("'{0}'", ((DateTime?)o).Value.ToString("yyyy-MM-dd HH:mm:ss")));
            //_DbTypeMap.Add(typeof(TimeSpan?), o => o?.ToString());
        }

        public static string ConvertToString(object value)
        {
            //var typeCode = Type.GetTypeCode(value.GetType());
            //switch (typeCode)
            //{
            //    case TypeCode.Boolean:
            //        return (bool)value ? "1" : "0";
            //    case TypeCode.Int16:
            //    case TypeCode.Int32:
            //    case TypeCode.Int64:
            //    case TypeCode.UInt16:
            //    case TypeCode.UInt32:
            //    case TypeCode.UInt64:
            //        return value.ToString();
            //    case TypeCode.DateTime:
            //        return ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss");
            //    default:
            //        return value == null ? "null" : $"'{value.ToString().Replace("'", "\'") }'";
            //}
            if (value == null)
                return "null";
            var type = value.GetType();
            if(_DbTypeMap.TryGetValue(type, out var kv))
            {
                return kv(value);
            }
            else
            {
                if (type.BaseType == typeof(Enum))
                {
                    Enum e = (Enum)value;
                    return e.ToString("D");
                }
                else
                {
                    return value == null ? "null" : $"'{value.ToString().Replace("'", "\'") }'";
                }
            }
        }

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

            //检查属性是否都是virtual 
            foreach (var p in DbTypeProperties.Values)
            {
                if (p.IsKey)
                {
                    if (p.PropertyInfo.SetMethod.Attributes.HasFlag(MethodAttributes.Virtual))
                    {
                        throw new SmartDbDomainReflectException(type, $"Key Property [{p.PropertyInfo.Name}] can't be virtual");
                    }
                }
                else
                {
                    if (!p.PropertyInfo.SetMethod.Attributes.HasFlag(MethodAttributes.Virtual))
                    {
                        throw new SmartDbDomainReflectException(type, $"Normal Property [{p.PropertyInfo.Name}] isnot virtual");
                    }
                }


                if (!_DbTypeMap.ContainsKey(p.PropertyInfo.PropertyType))
                {
                    if(p.PropertyInfo.PropertyType.BaseType != typeof(Enum)
                        && !(p.PropertyInfo.PropertyType.IsGenericType && p.PropertyInfo.PropertyType.Name.StartsWith("JsonObj")))
                    {
                        throw new SmartDbDomainReflectException(type, $"Property [{p.PropertyInfo.Name}] Type [{p.PropertyInfo.PropertyType}] is not support!");
                    }
                }
            }

            var propnames = properties.Select(p => p.Name);
            string sqlprop = string.Join(",", propnames.Select(p => $"`{p}`"));
            string sqlpropValue = string.Join(",", propnames.Select((p, i) => $"{{{i}}}"));
            InsertSqlFormat = $"insert into {type.Name}({sqlprop}) values({sqlpropValue});";
            var keys = DbTypeProperties.Values.Where(p => p.IsKey).ToList();
            if (keys.Count == 0)
            {
                throw new SmartDbDomainReflectException(type, $"缺少主键");
            }
            string whereKeys = string.Join(" and ", keys.Select((p, i) => $"`{p.PropertyInfo.Name}`={{{i}}}"));
            DeleteSqlFormat = $"delete from {type.Name} where {whereKeys}";
            UpdateSqlFormat = $"update {type.Name} set `{{{keys.Count}}}`={{{keys.Count+1}}} where {whereKeys}";
        }

        /// <summary>
        /// 得到所有属性的值
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public string[] GetAllValues(object item)
        {
            return DbTypeProperties.Values
                .Select((p, i) =>
                    {
                        return ConvertToString(p.PropertyInfo.GetValue(item));
                        //if (p.PropertyInfo.PropertyType.BaseType == typeof(Enum))
                        //{
                        //    Enum e = (Enum)p.PropertyInfo.GetValue(item);
                        //    return e.ToString("D");
                        //}
                        //else if (p.PropertyInfo.PropertyType == typeof(bool))
                        //{
                        //    bool e = (bool)p.PropertyInfo.GetValue(item);
                        //    return e ? "1" : "0";
                        //}else if (p.PropertyInfo.PropertyType == typeof(DateTime))
                        //{
                        //    DateTime e = (DateTime)p.PropertyInfo.GetValue(item);
                        //    return $"'{e.ToString("yyyy-MM-dd HH:mm:ss")}'";
                        //}
                        //var value = p.PropertyInfo.GetValue(item);
                        //return value == null ? "null": $"'{value.ToString().Replace("'","\'") }'";
                     })
                .ToArray();
        }

        /// <summary>
        /// 得到组成主键的属性的值
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public string[] GetKeyValues(object item)
        {
            return DbTypeProperties.Values.Where(p=>p.IsKey)
                .Select((p, i) =>
                {
                    return ConvertToString(p.PropertyInfo.GetValue(item));
                    //if (p.PropertyInfo.PropertyType.BaseType == typeof(Enum))
                    //{
                    //    Enum e = (Enum)p.PropertyInfo.GetValue(item);
                    //    return e.ToString("D");
                    //}
                    //else if (p.PropertyInfo.PropertyType == typeof(bool))
                    //{
                    //    bool e = (bool)p.PropertyInfo.GetValue(item);
                    //   return e ? "1" : "0";
                    //}
                    //else if (p.PropertyInfo.PropertyType == typeof(DateTime))
                    //{
                    //    DateTime e = (DateTime)p.PropertyInfo.GetValue(item);
                    //    return $"'{e.ToString("yyyy-MM-dd HH:mm:ss")}'";
                    //}
                    //var value = p.PropertyInfo.GetValue(item);
                    //return value == null ? "null" : $"'{value.ToString().Replace("'", "\'") }'";
                }).ToArray();
        }
    }
}
