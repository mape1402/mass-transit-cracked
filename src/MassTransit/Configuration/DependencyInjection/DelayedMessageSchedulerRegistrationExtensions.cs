namespace MassTransit
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;


    public static class DelayedMessageSchedulerRegistrationExtensions
    {
        /// <summary>
        /// Add a <see cref="IMessageScheduler" /> to the container that uses transport message delay to schedule messages
        /// </summary>
        /// <param name="configurator"></param>
        public static void AddDelayedMessageScheduler(this IRegistrationConfigurator configurator)
        {
            configurator.TryAddScoped(provider =>
            {
                var bus = provider.GetRequiredService<IBus>();
                var sendEndpointProvider = provider.GetRequiredService<ISendEndpointProvider>();
                return sendEndpointProvider.CreateDelayedMessageScheduler(bus.Topology);
            });
        }
    }
}
