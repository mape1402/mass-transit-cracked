﻿namespace MassTransit.ActiveMqTransport
{
    using System;
    using System.Threading.Tasks;
    using Apache.NMS;
    using Topology;


    public class ActiveMqMoveTransport
    {
        readonly Queue _destination;
        readonly IFilter<SessionContext> _topologyFilter;

        protected ActiveMqMoveTransport(Queue destination, IFilter<SessionContext> topologyFilter)
        {
            _topologyFilter = topologyFilter;
            _destination = destination;
        }

        protected async Task Move(ReceiveContext context, Action<IMessage, SendHeaders> preSend)
        {
            if (!context.TryGetPayload(out SessionContext sessionContext))
                throw new ArgumentException("The ReceiveContext must contain a SessionContext", nameof(context));

            if (!context.TryGetPayload(out ActiveMqMessageContext messageContext))
                throw new ArgumentException("The ActiveMqMessageContext was not present", nameof(context));

            await _topologyFilter.Send(sessionContext, Pipe.Empty<SessionContext>()).ConfigureAwait(false);

            var queue = await sessionContext.GetQueue(_destination).ConfigureAwait(false);

            var producer = await sessionContext.CreateMessageProducer(queue).ConfigureAwait(false);

            var message = messageContext.TransportMessage switch
            {
                IBytesMessage _ => producer.CreateBytesMessage(context.Body.GetBytes()),
                ITextMessage _ => producer.CreateTextMessage(context.Body.GetString()),
                _ => producer.CreateMessage(),
            };

            CloneMessage(message, messageContext.TransportMessage, preSend);

            var task = Task.Run(() => producer.Send(message));
            context.AddReceiveTask(task);
        }

        static void CloneMessage(IMessage message, IMessage source, Action<IMessage, SendHeaders> preSend)
        {
            message.NMSReplyTo = source.NMSReplyTo;
            message.NMSDeliveryMode = source.NMSDeliveryMode;
            message.NMSCorrelationID = source.NMSCorrelationID;
            message.NMSPriority = source.NMSPriority;

            foreach (string key in source.Properties.Keys)
                message.Properties[key] = source.Properties[key];

            SendHeaders headers = new PrimitiveMapHeaders(message.Properties);

            headers.SetHostHeaders();

            preSend(message, headers);
        }
    }
}
