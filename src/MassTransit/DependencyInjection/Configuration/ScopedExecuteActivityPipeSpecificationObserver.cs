namespace MassTransit.Configuration
{
    using System;
    using DependencyInjection;
    using Internals;
    using Middleware;


    public class ScopedExecuteActivityPipeSpecificationObserver :
        IActivityConfigurationObserver
    {
        readonly Type _filterType;
        readonly CompositeFilter<Type> _messageTypeFilter;
        readonly IServiceProvider _provider;

        public ScopedExecuteActivityPipeSpecificationObserver(Type filterType, IServiceProvider provider, CompositeFilter<Type> messageTypeFilter)
        {
            _filterType = filterType;
            _provider = provider;
            _messageTypeFilter = messageTypeFilter;
        }

        public void ActivityConfigured<TActivity, TArguments>(IExecuteActivityConfigurator<TActivity, TArguments> configurator, Uri compensateAddress)
            where TActivity : class, IExecuteActivity<TArguments>
            where TArguments : class
        {
            ExecuteActivityConfigured(configurator);
        }

        public void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityConfigurator<TActivity, TArguments> configurator)
            where TActivity : class, IExecuteActivity<TArguments>
            where TArguments : class
        {
            if (!_messageTypeFilter.Matches(typeof(TArguments)))
                return;

            var filterType = _filterType.MakeGenericType(typeof(TArguments));

            if (!filterType.HasInterface(typeof(IFilter<ExecuteContext<TArguments>>)))
                throw new ConfigurationException($"The scoped filter must implement {TypeCache<IFilter<ExecuteContext<TArguments>>>.ShortName} ");

            var scopeProvider = new ExecuteActivityScopeProvider<TActivity, TArguments>(_provider);

            var scopedFilterType = typeof(ScopedExecuteFilter<,,>).MakeGenericType(typeof(TActivity), typeof(TArguments), filterType);

            var filter = (IFilter<ExecuteContext<TArguments>>)Activator.CreateInstance(scopedFilterType, scopeProvider);

            var specification = new FilterPipeSpecification<ExecuteContext<TArguments>>(filter);

            configurator.Arguments(x => x.AddPipeSpecification(specification));
        }

        public void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityConfigurator<TActivity, TLog> configurator)
            where TActivity : class, ICompensateActivity<TLog>
            where TLog : class
        {
        }
    }
}
