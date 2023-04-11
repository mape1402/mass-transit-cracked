namespace MassTransit.Transports
{
    using System;
    using System.Collections.Concurrent;
    using System.Reflection;
    using System.Text;


    public class DefaultMessageNameFormatter :
        IMessageNameFormatter
    {
        readonly ConcurrentDictionary<Type, string> _cache;
        readonly string _genericArgumentSeparator;
        readonly string _genericTypeSeparator;
        readonly string _namespaceSeparator;
        readonly string _nestedTypeSeparator;

        public DefaultMessageNameFormatter(string genericArgumentSeparator, string genericTypeSeparator,
            string namespaceSeparator, string nestedTypeSeparator)
        {
            _genericArgumentSeparator = genericArgumentSeparator;
            _genericTypeSeparator = genericTypeSeparator;
            _namespaceSeparator = namespaceSeparator;
            _nestedTypeSeparator = nestedTypeSeparator;

            _cache = new ConcurrentDictionary<Type, string>();
        }

        public MessageName GetMessageName(Type type)
        {
            return new MessageName(_cache.GetOrAdd(type, CreateMessageName));
        }

        string CreateMessageName(Type type)
        {
            if (type.GetTypeInfo().IsGenericTypeDefinition)
                throw new ArgumentException("An open generic type cannot be used as a message name");

            var sb = new StringBuilder("");

            return GetMessageName(sb, type, null);
        }

        string GetMessageName(StringBuilder sb, Type type, string scope)
        {
            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsGenericParameter)
                return "";

            if (typeInfo.Namespace != null)
            {
                var ns = typeInfo.Namespace;
                if (!ns.Equals(scope))
                {
                    sb.Append(ns);
                    sb.Append(_namespaceSeparator);
                }
            }

            if (typeInfo.IsNested)
            {
                GetMessageName(sb, typeInfo.DeclaringType, typeInfo.Namespace);
                sb.Append(_nestedTypeSeparator);
            }

            if (typeInfo.IsGenericType)
            {
                var name = typeInfo.GetGenericTypeDefinition().Name;

                //remove `1
                var index = name.IndexOf('`');
                if (index > 0)
                    name = name.Remove(index);

                sb.Append(name);
                sb.Append(_genericTypeSeparator);

                Type[] arguments = typeInfo.GetGenericArguments();
                for (var i = 0; i < arguments.Length; i++)
                {
                    if (i > 0)
                        sb.Append(_genericArgumentSeparator);

                    GetMessageName(sb, arguments[i], typeInfo.Namespace);
                }

                sb.Append(_genericTypeSeparator);
            }
            else
                sb.Append(typeInfo.Name);

            return sb.ToString();
        }
    }
}
