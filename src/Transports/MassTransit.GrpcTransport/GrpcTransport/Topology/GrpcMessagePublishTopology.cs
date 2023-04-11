﻿#nullable enable
namespace MassTransit.GrpcTransport.Topology
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using MassTransit.Configuration;
    using MassTransit.Topology;
    using Transports.Fabric;


    public class GrpcMessagePublishTopology<TMessage> :
        MessagePublishTopology<TMessage>,
        IGrpcMessagePublishTopologyConfigurator<TMessage>
        where TMessage : class
    {
        readonly IList<IGrpcMessagePublishTopology> _implementedMessageTypes;
        readonly IMessageTopology<TMessage> _messageTopology;

        public GrpcMessagePublishTopology(IPublishTopology publishTopology, IMessageTopology<TMessage> messageTopology)
            : base(publishTopology)
        {
            _messageTopology = messageTopology;
            _implementedMessageTypes = new List<IGrpcMessagePublishTopology>();
        }

        public ExchangeType ExchangeType { get; set; }

        public void Apply(IMessageFabricPublishTopologyBuilder builder)
        {
            if (Exclude)
                return;

            var exchangeName = _messageTopology.EntityName;

            builder.ExchangeDeclare(exchangeName, ExchangeType);

            if (builder.ExchangeName != null)
                builder.ExchangeBind(builder.ExchangeName, exchangeName, builder.ExchangeType == ExchangeType.Topic ? "#" : default);
            else
            {
                builder.ExchangeName = exchangeName;
                builder.ExchangeType = ExchangeType;
            }

            foreach (var configurator in _implementedMessageTypes)
                configurator.Apply(builder);
        }

        public override bool TryGetPublishAddress(Uri baseAddress, [NotNullWhen(true)] out Uri? publishAddress)
        {
            publishAddress = new GrpcEndpointAddress(new GrpcHostAddress(baseAddress), _messageTopology.EntityName, exchangeType: ExchangeType);
            return true;
        }

        public void AddImplementedMessageConfigurator<T>(IGrpcMessagePublishTopologyConfigurator<T> configurator, bool direct)
            where T : class
        {
            var adapter = new TypeAdapter<T>(configurator, direct);

            _implementedMessageTypes.Add(adapter);
        }


        class TypeAdapter<T> :
            IGrpcMessagePublishTopology
            where T : class
        {
            readonly IGrpcMessagePublishTopologyConfigurator<T> _configurator;
            readonly bool _direct;

            public TypeAdapter(IGrpcMessagePublishTopologyConfigurator<T> configurator, bool direct)
            {
                _configurator = configurator;
                _direct = direct;
            }

            public void Apply(IMessageFabricPublishTopologyBuilder builder)
            {
                if (_direct)
                {
                    var implementedBuilder = builder.CreateImplementedBuilder();

                    _configurator.Apply(implementedBuilder);
                }
            }
        }
    }
}
