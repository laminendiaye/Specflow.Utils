using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Common.Services;
using TechTalk.SpecFlow;

namespace Specflow.Converters
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    public sealed class CustomInnerProperty : Attribute
    {
    }

    [Serializable]
    public class MappingException : Exception
    {
        public MappingException(string message) : base(message)
        {
        }

        public MappingException(string message, Exception inner) : base(message, inner)
        {
        }

        protected MappingException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }

    class PropertyTypeInfo
    {
        public PropertyInfo PropertyInfo { get; set; }
        public PropertyInnerType PropertyInnerType { get; set; }
    }

    internal enum PropertyInnerType
    {
        Inner,
        InnerCustom
    }

    class TypeInfoContainer
    {
        public static TypeInfoContainer Create(Type type)
        {
            return new TypeInfoContainer(type);
        }

        private readonly Type _type;
        private readonly List<PropertyInfo> _propertyInfos;
        private readonly List<string> _propertyNames;
        private readonly List<TypeInfoContainer> _innerContainers;

        private TypeInfoContainer(Type type)
        {
            _type = type;
            _propertyInfos = new List<PropertyInfo>();
            _propertyNames = new List<string>();
            _innerContainers = new List<TypeInfoContainer>();
            InitMappings();
        }

        private void InitMappings()
        {
            PropertyInfo[] propertyInfos = _type.GetProperties();
            _propertyInfos.AddRange(propertyInfos);
            _propertyNames.AddRange(_propertyInfos.Select(p => p.Name));

            //TODO: Gerer les conflit de noms de proprietes et de reference circulaire

            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                var customInnerProperties = propertyInfo.GetCustomAttributes<CustomInnerProperty>();
                if (customInnerProperties.Any())
                {
                    _innerContainers.Add(Create(propertyInfo.PropertyType));
                }
            }
        }

        public IEnumerable<PropertyInfo> PropertyInfos
        {
            get
            {
                return _propertyInfos;
            }
        }

        public IEnumerable<string> PropertyNames
        {
            get { return _propertyNames; }
        }

        public List<TypeInfoContainer> InnerContainers
        {
            get { return _innerContainers; }
        }

        public IEnumerable<PropertyInfo> AllPropertyInfos
        {
            get
            {
                List<PropertyInfo> results = new List<PropertyInfo>();
                results.AddRange(_propertyInfos);
                results.AddRange(_innerContainers.SelectMany(c => c.PropertyInfos));
                return results;
            }
        }

        public IEnumerable<string> AllPropertyNames
        {
            get
            {
                List<string> results = new List<string>();
                results.AddRange(_propertyNames);
                results.AddRange(_innerContainers.SelectMany(c => c.PropertyNames));
                return results;
            }
        }
    }

    class MappingSettings
    {
        public MappingBehaviour MappingBehaviour { get; set; }
    }

    public enum MappingBehaviour
    {
        Loose,
        Strict
    }

    static class TypeMapping
    {
        private static readonly IDictionary<Type, TypeInfoContainer> _typeInfosPerType;
        private static readonly MappingSettings _settings;

        static TypeMapping()
        {
            _typeInfosPerType = new Dictionary<Type, TypeInfoContainer>();
            _settings = new MappingSettings();
        }

        public static MappingSettings Settings
        {
            get { return _settings; }
        }

        public static void RegisterType<T>()
            where T : class, new()
        {
            Type type = typeof(T);
            _typeInfosPerType[type] = TypeInfoContainer.Create(type);
        }

        public static bool ContainsType(Type type)
        {
            return _typeInfosPerType.ContainsKey(type);
        }
        public static TypeInfoContainer GetTypeInfosContainer(Type type)
        {
            return _typeInfosPerType[type];
        }
    }

    //TODO: Under implementation. Does not fully work yet
    public static class TableRowExtension
    {
        public static T CreateInstance<T>(this TableRow row)
            where T : class, new()
        {
            return Instance<T>(row, TypeMapping.Settings.MappingBehaviour);
        }

        public static T CreateInstance<T>(this TableRow row, MappingBehaviour behaviour)
            where T : class, new()
        {
            return Instance<T>(row, behaviour);
        }

        private static T Instance<T>(TableRow row, MappingBehaviour mappingBehaviour) where T : class, new()
        {
            Type type = typeof(T);
            if (!TypeMapping.ContainsType(type))
            {
                TypeMapping.RegisterType<T>();
            }

            var container = TypeMapping.GetTypeInfosContainer(type);
            switch (mappingBehaviour)
            {
                case MappingBehaviour.Loose:
                    break;
                case MappingBehaviour.Strict:
                    StringBuilder sb = new StringBuilder();
                    var headersExceptPropertyNames = row.Keys.Except(container.PropertyNames).ToArray();
                    if (headersExceptPropertyNames.Any())
                    {
                        sb.AppendLine();
                        sb.AppendFormat(
                            "** The following column(s) are present in the table row but not the target type '{0}': {1}",
                            type.Name, headersExceptPropertyNames.JoinStrings());
                    }
                    var propertyNamesExceptHeaders = container.PropertyNames.Except(row.Keys).ToArray();
                    if (propertyNamesExceptHeaders.Any())
                    {
                        sb.AppendLine();
                        sb.AppendFormat(
                            "** The following column(s) are present in the target type '{0}' but not in the table row: {1}",
                            type.Name, propertyNamesExceptHeaders.JoinStrings());
                    }
                    string message = sb.ToString();
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        throw new MappingException(message);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(string.Format("Mapping behaviour '{0}' not managed", mappingBehaviour));
            }
            var instance = Activator.CreateInstance<T>();
            foreach (PropertyInfo propertyInfo in container.PropertyInfos)
            {
                object value = TypeServices.ChangeType(row[propertyInfo.Name], propertyInfo.PropertyType);
                propertyInfo.SetValue(instance, value);
            }

            //var instance = TypeServices.Resolve<T>();
            //foreach (PropertyInfo propertyInfo in container.AllPropertyInfos)
            //{
            //    object value = TypeServices.ChangeType(row[propertyInfo.Name], propertyInfo.PropertyType);
            //    propertyInfo.SetValue(instance, value);
            //}

            return instance;
        }
    }

    public static class TableRowConverter
    {
        public static T ToObject<T>(TableRow tableRow)
        {
            PropertyInfo[] propertyInfos = typeof(T).GetProperties();
            T target = Activator.CreateInstance<T>();
            foreach (PropertyInfo pi in propertyInfos)
            {
                var piType = pi.PropertyType;
                var piName = pi.Name;
                object value = TypeServices.ChangeType(tableRow[piName], piType);
                pi.SetValue(target, value);
            }
            return target;
        }
    }
}
