#nullable enable
namespace MassTransit
{
    using System;


    public interface IGrpcBusFactoryConfigurator :
        IBusFactoryConfigurator<IGrpcReceiveEndpointConfigurator>
    {
        new IGrpcPublishTopologyConfigurator PublishTopology { get; }

        /// <summary>
        /// Configure the send topology of the message type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configureTopology"></param>
        void Publish<T>(Action<IGrpcMessagePublishTopologyConfigurator<T>>? configureTopology)
            where T : class;

        void Publish(Type messageType, Action<IGrpcMessagePublishTopologyConfigurator>? configure = null);

        /// <summary>
        /// Configure the base address for the host
        /// </summary>
        /// <param name="configure"></param>
        /// <returns></returns>
        void Host(Action<IGrpcHostConfigurator>? configure = null);

        /// <summary>
        /// Configure the base address for the host
        /// </summary>
        /// <param name="baseAddress">The base address for the in-memory host</param>
        /// <param name="configure"></param>
        /// <returns></returns>
        void Host(Uri baseAddress, Action<IGrpcHostConfigurator>? configure = null);
    }
}
