﻿namespace MassTransit.Configuration
{
    using System;
    using System.Threading.Tasks;
    using Observables;
    using Transports;


    public interface IReceiveEndpointConfiguration :
        IEndpointConfiguration,
        IReceiveEndpointObserverConnector,
        IReceiveEndpointDependentConnector
    {
        IConsumePipe ConsumePipe { get; }

        Uri HostAddress { get; }

        Uri InputAddress { get; }

        bool ConfigureConsumeTopology { get; }

        bool PublishFaults { get; }

        int PrefetchCount { get; }

        int? ConcurrentMessageLimit { get; }

        ReceiveEndpointObservable EndpointObservers { get; }
        ReceiveObservable ReceiveObservers { get; }
        ReceiveTransportObservable TransportObservers { get; }
        IReceiveEndpoint ReceiveEndpoint { get; }

        /// <summary>
        /// Completed once the receive endpoint dependencies are ready
        /// </summary>
        Task DependenciesReady { get; }

        /// <summary>
        /// Completed once the receive endpoint dependents are completed
        /// </summary>
        Task DependentsCompleted { get; }

        /// <summary>
        /// Create the receive pipe, using the endpoint configuration
        /// </summary>
        /// <returns></returns>
        IReceivePipe CreateReceivePipe();

        ReceiveEndpointContext CreateReceiveEndpointContext();
    }
}
