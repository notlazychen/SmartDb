using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SmartDb
{
    public class DbTypePropertyInfo
    {
        public PropertyInfo PropertyInfo { get; set; }
        public bool IsKey { get; set; }

        //public string ParameterName { get; set; }
        //public MySqlDbType SqlDbType { get; set; }
        public object GetValue(object obj)
        {
            return PropertyInfo.GetValue(obj);
        }
    }
}
