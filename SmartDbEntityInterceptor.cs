using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SmartDb
{
    public static class SmartDbEntityInterceptor
    {
        class DbFieldInfo
        {
            public FieldInfo FieldInfo { get; set; }
            public string PropertyName { get; set; }
        }
        private static Dictionary<Type, Dictionary<string, DbFieldInfo>> _fields =
            new Dictionary<Type, Dictionary<string, DbFieldInfo>>();

        public static void Invoke(object @object, string @method, object[] parameter)
        {
            var type = @object.GetType();
            if(!_fields.TryGetValue(type, out var dic))
            {
                dic = new Dictionary<string, DbFieldInfo>();
                IEnumerable<FieldInfo> fields = type.BaseType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);            
                foreach (var f in fields)
                {
                    int last = f.Name.IndexOf('>');
                    if(last <= 0)
                    {
                        continue;
                    }
                    string propertyName = f.Name.Substring(1, last - 1);
                    dic.Add($"set_{propertyName}", new DbFieldInfo { FieldInfo = f, PropertyName = propertyName });
                }
                _fields.Add(type, dic);
            }
            var field = dic[@method];
            field.FieldInfo.SetValue(@object, parameter[0]); 
            SmartDbBus.PlanToSaveUpdateToDb(@object, field.PropertyName, parameter[0]);
        }
    }
}

