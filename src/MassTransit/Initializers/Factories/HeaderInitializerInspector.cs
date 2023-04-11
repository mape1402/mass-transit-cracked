namespace MassTransit.Initializers.Factories
{
    using System.Reflection;
    using Conventions;


    public class HeaderInitializerInspector<TMessage, TInput, TProperty> :
        IHeaderInitializerInspector<TMessage, TInput>
        where TMessage : class
        where TInput : class
    {
        readonly PropertyInfo _propertyInfo;

        public HeaderInitializerInspector(PropertyInfo propertyInfo)
        {
            _propertyInfo = propertyInfo;
        }

        public bool Apply(IMessageInitializerBuilder<TMessage, TInput> builder, IInitializerConvention convention)
        {
            if (builder.IsInputPropertyUsed(_propertyInfo.Name))
                return false;

            if (convention.TryGetHeaderInitializer<TMessage, TInput, TProperty>(_propertyInfo, out IHeaderInitializer<TMessage, TInput> initializer))
            {
                builder.Add(initializer);

                builder.SetInputPropertyUsed(_propertyInfo.Name);
                return true;
            }

            return false;
        }
    }
}
