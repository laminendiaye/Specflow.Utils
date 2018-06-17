using System;
using System.Linq;
using System.Reflection;

namespace Common.Services
{
    public static class TypeServices
    {
        public static object ChangeType(string value, Type conversionType)
        {
            if (conversionType.IsEnum)
            {
                return Enum.Parse(conversionType, value);
            }
            return Convert.ChangeType(value, conversionType);
        }

        public static T Resolve<T>()
            where T : class, new()
        {
            Type type = typeof(T);
            return (T)Resolve(type);
        }

        private static object Resolve(Type type)
        {
            var instance = Activator.CreateInstance(type);
            PropertyInfo[] propertyInfos = type.GetProperties();

            if (propertyInfos.Any())
            {
                foreach (PropertyInfo propertyInfo in propertyInfos)
                {
                    var propertyType = propertyInfo.PropertyType;
                    ConstructorInfo constructorInfo = propertyType.GetConstructor(Type.EmptyTypes);
                    object propertyInstance;
                    if (constructorInfo != null)
                    {
                        propertyInstance = Resolve(propertyType);
                    }
                    else
                    {
                        propertyInstance = CreateDefault(propertyType);
                    }
                    propertyInfo.SetValue(instance, propertyInstance);
                }
            }
            return instance;
        }

        private static object CreateDefault(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }
    }
}