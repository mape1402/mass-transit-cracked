﻿namespace MassTransit
{
    using System;
    using RabbitMqTransport;


    /// <summary>
    /// Configure a receiving RabbitMQ endpoint
    /// </summary>
    public interface IRabbitMqReceiveEndpointConfigurator :
        IReceiveEndpointConfigurator,
        IRabbitMqQueueEndpointConfigurator
    {
        /// <summary>
        /// If false, deploys only exchange, without queue
        /// </summary>
        bool BindQueue { set; }

        /// <summary>
        /// Specifies the dead letter exchange name, which is used to send expired messages
        /// </summary>
        string DeadLetterExchange { set; }

        /// <summary>
        /// Bind an exchange to the receive endpoint exchange
        /// </summary>
        /// <param name="exchangeName">The exchange name</param>
        /// <param name="callback">Configure the exchange and binding</param>
        void Bind(string exchangeName, Action<IRabbitMqExchangeToExchangeBindingConfigurator> callback = null);

        /// <summary>
        /// Bind an exchange to the receive endpoint exchange
        /// </summary>
        /// <param name="callback">Configure the exchange and binding</param>
        void Bind<T>(Action<IRabbitMqExchangeBindingConfigurator> callback = null)
            where T : class;

        /// <summary>
        /// Bind a dead letter exchange and queue to the receive endpoint so that expired messages are moved automatically.
        /// </summary>
        /// <param name="exchangeName"></param>
        /// <param name="queueName"></param>
        /// <param name="configure"></param>
        void BindDeadLetterQueue(string exchangeName, string queueName = null, Action<IRabbitMqQueueBindingConfigurator> configure = null);

        /// <summary>
        /// Add middleware to the model pipe
        /// </summary>
        /// <param name="configure"></param>
        void ConfigureModel(Action<IPipeConfigurator<ModelContext>> configure);

        /// <summary>
        /// Add middleware to the connection pipe
        /// </summary>
        /// <param name="configure"></param>
        void ConfigureConnection(Action<IPipeConfigurator<ConnectionContext>> configure);

        /// <summary>
        /// By default, RabbitMQ assigns a dynamically generated consumer tag, which is always the right choice. In certain scenarios
        /// where a specific consumer tag is needed, this will set it.
        /// </summary>
        /// <param name="consumerTag">The consumer tag to use for this receive endpoint.</param>
        void OverrideConsumerTag(string consumerTag);
    }
}
