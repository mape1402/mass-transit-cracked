namespace MassTransit.Azure.ServiceBus.Core.Tests
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using DupeDetection;
    using MassTransit.Testing;
    using NUnit.Framework;
    using PublishContracts;


    [TestFixture]
    public class Publishing_to_a_duplicate_detection_topic :
        AzureServiceBusTestFixture
    {
        [Test]
        public async Task Should_succeed()
        {
            var messageId = NewId.NextGuid();

            await Bus.Publish<DupeCommand>(new {Value = "FirstValue"}, context => context.MessageId = messageId);
            await Bus.Publish<DupeCommand>(new {Value = "SecondValue"}, context => context.MessageId = messageId);

            var count = _consumer.Commands.Select(InactivityToken).Count();

            Assert.That(count, Is.EqualTo(1));
        }

        DupeConsumer _consumer;

        protected override void ConfigureServiceBusBus(IServiceBusBusFactoryConfigurator configurator)
        {
            configurator.Publish<DupeCommand>(x => x.EnableDuplicateDetection(TimeSpan.FromMinutes(10)));
        }

        protected override void ConfigureServiceBusReceiveEndpoint(IServiceBusReceiveEndpointConfigurator configurator)
        {
            _consumer = new DupeConsumer(TestTimeout, TestCancellationToken);

            _consumer.Configure(configurator);
        }


        class DupeConsumer :
            MultiTestConsumer
        {
            public DupeConsumer(TimeSpan timeout, CancellationToken testCompleted = default)
                : base(timeout, testCompleted)
            {
                Commands = Consume<DupeCommand>();
            }

            public ReceivedMessageList<DupeCommand> Commands { get; }
        }
    }


    namespace DupeDetection
    {
        public interface DupeCommand
        {
            string Value { get; }
        }
    }


    namespace PublishContracts
    {
        using System;


        public interface OrderSubmitted :
            OrderEvent
        {
        }


        public interface OrderEvent
        {
            public Guid OrderId { get; }
        }


        public interface PackageShipped :
            PackageEvent
        {
        }


        [ExcludeFromTopology]
        public interface PackageEvent
        {
        }


        public interface CustomerEvent
        {
            public Guid CustomerId { get; }
        }
    }


    public class Configuring_the_publish_topology_at_startup :
        AzureServiceBusTestFixture
    {
        [Test]
        [Explicit]
        public void Should_create_the_exchanges()
        {
        }

        protected override void ConfigureServiceBusBus(IServiceBusBusFactoryConfigurator configurator)
        {
            configurator.DeployPublishTopology = true;

            configurator.Publish<OrderSubmitted>();
            configurator.Publish<PackageShipped>();
        }
    }


    public class Configuring_the_publish_topology_at_startup_by_namespace :
        AzureServiceBusTestFixture
    {
        [Test]
        [Explicit]
        public void Should_create_the_exchanges()
        {
        }

        protected override void ConfigureServiceBusBus(IServiceBusBusFactoryConfigurator configurator)
        {
            configurator.DeployPublishTopology = true;

            configurator.AddPublishMessageTypesFromNamespaceContaining<OrderSubmitted>();
        }
    }

}
