using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace SmartDb
{

    public delegate TTarget MapMethod<TTarget, TSource>(TSource source);


    public static class FastMapper<TTarget, TSource>
    {
        private static MapMethod<TTarget, TSource> mapMethod;

        public static MapMethod<TTarget, TSource> GetMapMethod()
        {
            if (mapMethod == null)
            {
                mapMethod = CreateMapMethod(typeof(TTarget), typeof(TSource));
            }
            return mapMethod;
        }

        public static TTarget Map(TSource source)
        {
            if (mapMethod == null)
            {
                mapMethod = CreateMapMethod(typeof(TTarget), typeof(TSource));
            }
            return mapMethod(source);
        }

        private static MapMethod<TTarget, TSource> CreateMapMethod(Type targetType, Type sourceType)
        {


            DynamicMethod map = new DynamicMethod("Map", targetType, new Type[] { sourceType }, typeof(TTarget).Module);

            ILGenerator il = map.GetILGenerator();
            ConstructorInfo ci = targetType.GetConstructor(new Type[0]);
            il.DeclareLocal(targetType);
            il.Emit(OpCodes.Newobj, ci);
            il.Emit(OpCodes.Stloc_0);
            foreach (var sourcePropertyInfo in sourceType.GetProperties())
            {
                var targetPropertyInfo = (from p in targetType.GetProperties()
                                          where p.Name == sourcePropertyInfo.Name && p.PropertyType == sourcePropertyInfo.PropertyType
                                          select p).FirstOrDefault();

                if (targetPropertyInfo == null) continue;

                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Callvirt, sourcePropertyInfo.GetGetMethod());
                il.Emit(OpCodes.Callvirt, targetPropertyInfo.GetSetMethod());

            }
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ret);

            return (MapMethod<TTarget, TSource>)map.CreateDelegate(typeof(MapMethod<TTarget, TSource>));

        }
    }
}
