namespace MassTransit.Middleware
{
    using System;


    public class OutboxConsumeOptions
    {
        /// <summary>
        /// The generated identifier for the consumer based upon endpoint name
        /// </summary>
        public Guid ConsumerId { get; set; }

        /// <summary>
        /// The display name of the consumer type
        /// </summary>
        public string ConsumerType { get; set; }

        /// <summary>
        /// The number of message to deliver at a time from the outbox
        /// </summary>
        public int MessageDeliveryLimit { get; set; }

        /// <summary>
        /// The time to wait when delivering a message to the broker
        /// </summary>
        public TimeSpan MessageDeliveryTimeout { get; set; }
    }
}
