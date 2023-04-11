namespace MassTransit.RabbitMqTransport.Tests
{
    using System.Threading.Tasks;
    using NUnit.Framework;
    using TestFramework.Messages;


    [TestFixture]
    [Category("Flaky")]
    public class Using_the_reply_to_address :
        RabbitMqTestFixture
    {
        [Test]
        public async Task Should_deliver_the_response()
        {
            var clientFactory = await Bus.CreateReplyToClientFactory();

            IRequestClient<PingMessage> client = clientFactory.CreateRequestClient<PingMessage>(InputQueueAddress, TestTimeout);

            Response<PongMessage> response = await client.GetResponse<PongMessage>(new PingMessage());
        }

        protected override void ConfigureRabbitMqReceiveEndpoint(IRabbitMqReceiveEndpointConfigurator configurator)
        {
            configurator.Handler<PingMessage>(x => x.RespondAsync<PongMessage>(x.Message));
        }
    }
}
