﻿namespace MassTransit.ActiveMqTransport.Configuration
{
    using MassTransit.Configuration;
    using Observables;


    public class ActiveMqBusConfiguration :
        ActiveMqEndpointConfiguration,
        IActiveMqBusConfiguration
    {
        readonly BusObservable _busObservers;

        public ActiveMqBusConfiguration(IActiveMqTopologyConfiguration topologyConfiguration)
            : base(topologyConfiguration)
        {
            HostConfiguration = new ActiveMqHostConfiguration(this, topologyConfiguration);
            BusEndpointConfiguration = CreateEndpointConfiguration(true);

            _busObservers = new BusObservable();
        }

        IHostConfiguration IBusConfiguration.HostConfiguration => HostConfiguration;
        IEndpointConfiguration IBusConfiguration.BusEndpointConfiguration => BusEndpointConfiguration;
        IBusObserver IBusConfiguration.BusObservers => _busObservers;

        public IActiveMqEndpointConfiguration BusEndpointConfiguration { get; }
        public IActiveMqHostConfiguration HostConfiguration { get; }

        public ConnectHandle ConnectBusObserver(IBusObserver observer)
        {
            return _busObservers.Connect(observer);
        }

        public ConnectHandle ConnectEndpointConfigurationObserver(IEndpointConfigurationObserver observer)
        {
            return HostConfiguration.ConnectEndpointConfigurationObserver(observer);
        }
    }
}
