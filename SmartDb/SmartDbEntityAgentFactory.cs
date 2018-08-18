using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace SmartDb
{
    public static class SmartDbEntityAgentFactory
    {
        private static Dictionary<Type, Type> _agentTypeMap = new Dictionary<Type, Type>();

        public static Type OfType(Type type)
        {
            if (!_agentTypeMap.TryGetValue(type, out var t))
            {
                string nameOfAssembly = type.Name + "ProxyAssembly";
                string nameOfModule = type.Name + "ProxyModule";
                string nameOfType = type.Name + "Proxy";

                var assemblyName = new AssemblyName(nameOfAssembly);

                var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                //var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                var moduleBuilder = assembly.DefineDynamicModule(nameOfModule);

                var typeBuilder = moduleBuilder.DefineType(
                  nameOfType, TypeAttributes.Public, type);

                InjectInterceptor(type, typeBuilder);

                t = typeBuilder.CreateTypeInfo();
                _agentTypeMap.Add(type, t);
            }
            return t;
        }

        public static T Of<T>() where T : class, new()
        {
            var type = typeof(T);
            var t = OfType(type);
            return Activator.CreateInstance(t) as T;
        }

        public static T Of<T>(T org) where T : class, new()
        {
            var type = typeof(T);
            if (type.Name.EndsWith("Proxy"))
            {
                throw new Exception("不能托管已是托管的类型");
            }
            var t = OfType(type);
            T obj = Activator.CreateInstance(t) as T;

            //赋初值
            IEnumerable<FieldInfo> fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var f in fields)
            {
                //包含那些不需要托管的字段
                //int last = f.Name.IndexOf('>');
                //if (last <= 0)
                //{
                //    continue;
                //}
                //string propertyName = f.Name.Substring(1, last - 1);
                f.SetValue(obj, f.GetValue(org));
            }
            var curt = type.BaseType;
            while (curt != typeof(object))//那些继承的值
            {
                fields = curt.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var f in fields)
                {
                    //包含那些不需要托管的字段
                    //int last = f.Name.IndexOf('>');
                    //if (last <= 0)
                    //{
                    //    continue;
                    //}
                    //string propertyName = f.Name.Substring(1, last - 1);
                    f.SetValue(obj, f.GetValue(org));
                }
                curt = curt.BaseType;
            }

            return obj;
        }

        private static void InjectInterceptor(Type t, TypeBuilder typeBuilder)
        {
            // ---- define fields ----
            var interceptorType = typeof(SmartDbEntityInterceptor);
            //var fieldInterceptor = typeBuilder.DefineField(
            //  "_interceptor", interceptorType, FieldAttributes.Private);

            // ---- define costructors ----

            var constructorBuilder = typeBuilder.DefineConstructor(
              MethodAttributes.Public, CallingConventions.Standard, null);
            var ilOfCtor = constructorBuilder.GetILGenerator();

            ilOfCtor.Emit(OpCodes.Ldarg_0);
            ilOfCtor.Emit(OpCodes.Call, t.GetConstructor(new Type[0]));
            ilOfCtor.Emit(OpCodes.Nop);
            //ilOfCtor.Emit(OpCodes.Newobj, interceptorType.GetConstructor(new Type[0]));
            //ilOfCtor.Emit(OpCodes.Stfld, fieldInterceptor);
            ilOfCtor.Emit(OpCodes.Ret);

            // ---- define methods ----

            //获取属性Set方法
            IEnumerable<MethodInfo> methodsOfType = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty | BindingFlags.NonPublic);
            methodsOfType = methodsOfType.Where(m => m.Attributes.HasFlag(MethodAttributes.HideBySig | MethodAttributes.SpecialName) && m.Name.StartsWith("set_"));

            foreach (var method in methodsOfType)
            {
                var methodParameterTypes =
                  method.GetParameters().Select(p => p.ParameterType).ToArray();
                var methodBuilder = typeBuilder.DefineMethod(
                    method.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual,
                    CallingConventions.Standard,
                    method.ReturnType,
                    methodParameterTypes);
                //var methodBuilder = typeBuilder.DefineMethod(
                //   method.Name,
                //   method.Attributes,
                //   method.CallingConvention,
                //   method.ReturnType,
                //   methodParameterTypes);

                var ilOfMethod = methodBuilder.GetILGenerator();
                ilOfMethod.Emit(OpCodes.Ldarg_0);
                //ilOfMethod.Emit(OpCodes.Ldfld, fieldInterceptor);

                // create instance of T
                //ilOfMethod.Emit(OpCodes.Newobj, typeof(T).GetConstructor(new Type[0]));
                ilOfMethod.Emit(OpCodes.Ldstr, method.Name);

                // build the method parameters
                if (methodParameterTypes == null)
                {
                    ilOfMethod.Emit(OpCodes.Ldnull);
                }
                else
                {
                    var parameters = ilOfMethod.DeclareLocal(typeof(object[]));
                    ilOfMethod.Emit(OpCodes.Ldc_I4, methodParameterTypes.Length);
                    ilOfMethod.Emit(OpCodes.Newarr, typeof(object));
                    ilOfMethod.Emit(OpCodes.Stloc, parameters);

                    for (var j = 0; j < methodParameterTypes.Length; j++)
                    {
                        ilOfMethod.Emit(OpCodes.Ldloc, parameters);
                        ilOfMethod.Emit(OpCodes.Ldc_I4, j);
                        ilOfMethod.Emit(OpCodes.Ldarg, j + 1);
                        if (methodParameterTypes[j].IsValueType)
                        {
                            ilOfMethod.Emit(OpCodes.Box, methodParameterTypes[j]);
                        }
                        ilOfMethod.Emit(OpCodes.Stelem_Ref);
                    }
                    ilOfMethod.Emit(OpCodes.Ldloc, parameters);
                }

                // call Invoke() method of Interceptor
                var mi = interceptorType.GetMethod("Invoke");
                ilOfMethod.Emit(OpCodes.Call, mi);

                //ilOfMethod.Emit(OpCodes.Pop);
                //ilOfMethod.Emit(OpCodes.Ldarg_0);
                //ilOfMethod.Emit(OpCodes.Ldarg_1);
                //var mi2 = typeof(T).GetMethod(method.Name);
                //ilOfMethod.Emit(OpCodes.Callvirt, mi2);
                //ilOfMethod.Emit(OpCodes.Nop);

                // pop the stack if return void
                //if (method.ReturnType == typeof(void))
                //{
                //    ilOfMethod.Emit(OpCodes.Pop);
                //}
                ilOfMethod.Emit(OpCodes.Nop);

                // complete
                ilOfMethod.Emit(OpCodes.Ret);

                //PropertyBuilder pbNumber = typeBuilder.DefineProperty(
                //     "Name",
                //     PropertyAttributes.HasDefault,
                //     typeof(int),
                //     null);
                //pbNumber.SetSetMethod(methodBuilder);

            }
        }
    }
}
