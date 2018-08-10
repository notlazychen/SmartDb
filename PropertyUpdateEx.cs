using System;
using System.Collections.Generic;
using System.Text;

namespace SmartDb
{
    public static class PropertyUpdateEx
    {
        public static T SetValue<T>(this ref T org, T dest) where T : struct
        {
            string name = nameof(org);
            return dest;
        }
    }
}
