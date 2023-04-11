namespace MassTransit.Azure.ServiceBus.Core.Tests
{
    namespace ConductorTests
    {
        using System.Threading.Tasks;
        using Contracts;
        using NUnit.Framework;


        namespace Contracts
        {
            using System;


            public interface DeployHappiness
            {
                string Target { get; }
            }


            public interface DeployPayload
            {
                string Target { get; }
            }


            public interface PayloadDeployed
            {
                DateTime Timestamp { get; }
                string Target { get; }
            }
        }


        public class DeployPayloadConsumer :
            IConsumer<DeployPayload>
        {
            public Task Consume(ConsumeContext<DeployPayload> context)
            {
                return context.RespondAsync<PayloadDeployed>(new
                {
                    InVar.Timestamp,
                    context.Message.Target
                });
            }
        }


        [TestFixture]
        public class Using_conductor_for_service_discovery :
            AzureServiceBusTestFixture
        {
            [Test]
            public async Task Should_connect_using_the_service_client()
            {
                IRequestClient<DeployPayload> requestClient = Bus.CreateRequestClient<DeployPayload>();

                Response<PayloadDeployed> response = await requestClient.GetResponse<PayloadDeployed>(new {Target = "Bogey"});

                Assert.That(response.Message.Target, Is.EqualTo("Bogey"));
            }

            protected override void ConfigureServiceBusBus(IServiceBusBusFactoryConfigurator configurator)
            {
                configurator.ServiceInstance(instance =>
                {
                    var serviceEndpointName = KebabCaseEndpointNameFormatter.Instance.Consumer<DeployPayloadConsumer>();

                    instance.ReceiveEndpoint(serviceEndpointName, x =>
                    {
                        x.Consumer<DeployPayloadConsumer>();
                    });
                });
            }
        }
    }
}
