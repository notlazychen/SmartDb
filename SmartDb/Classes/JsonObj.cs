using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartDb
{
    public class JsonObj<T>
        where T:class
    {
        private T _Obj;
        public T Obj { get { return _Obj; } }

        public JsonObj(T t)
        {
            _Obj = t;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(_Obj);
        }

        //public static bool operator ==(JsonObj<T> a, JsonObj<T> b)
        //{

        //}
        //public static bool operator !=(JsonObject<T> a, JsonObject<T> b);
        
        public static implicit operator JsonObj<T>(T obj)
        {
            return new JsonObj<T>(obj);
        }

        public static implicit operator T(JsonObj<T> jobj)
        {
            return jobj._Obj;
        }

        //public static implicit operator JsonObj<T>(byte[] json)
        //{
        //    T obj = JsonConvert.DeserializeObject<T>(json);
        //    return new JsonObj<T>(obj);
        //}

        public static implicit operator JsonObj<T>(string json)
        {
            T obj = JsonConvert.DeserializeObject<T>(json);
            return new JsonObj<T>(obj);
        }
    }
}
