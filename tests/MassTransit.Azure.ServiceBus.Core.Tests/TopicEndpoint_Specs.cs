namespace MassTransit.Azure.ServiceBus.Core.Tests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;


    [TestFixture]
    public class Sending_to_a_topic_endpoint :
        AzureServiceBusTestFixture
    {
        [Test]
        public async Task Should_succeed()
        {
            var endpoint = await Bus.GetSendEndpoint(new Uri("topic:private-topic"));
            await endpoint.Send(new PrivateMessage {Value = "Hello"});

            ConsumeContext<PrivateMessage> context = await _handler;

            Assert.That(context.Message.Value, Is.EqualTo("Hello"));
        }

        Task<ConsumeContext<PrivateMessage>> _handler;

        protected override void ConfigureServiceBusReceiveEndpoint(IServiceBusReceiveEndpointConfigurator configurator)
        {
            configurator.ConfigureConsumeTopology = false;

            configurator.Subscribe("private-topic", "input-queue-subscription");

            _handler = Handled<PrivateMessage>(configurator);
        }


        class PrivateMessage
        {
            public string Value { get; set; }
        }
    }
}
