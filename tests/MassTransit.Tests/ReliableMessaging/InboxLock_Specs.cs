namespace MassTransit.Tests.ReliableMessaging
{
    using System;
    using System.Threading.Tasks;
    using InboxLock;
    using Logging;
    using MassTransit.Testing;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;


    [TestFixture]
    public class When_multiple_deliveries_of_the_same_message_occur
    {
        [Test]
        public async Task Should_block_subsequent_consumers_by_lock()
        {
            await using var provider = new ServiceCollection()
                .AddInboxLockInMemoryTestHarness()
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();
            harness.TestInactivityTimeout = TimeSpan.FromSeconds(5);

            await harness.Start();

            var messageId = NewId.NextGuid();

            await harness.Bus.Publish(new Command(), x => x.MessageId = messageId);
            await Task.Delay(5);

            await harness.Bus.Publish(new Command(), x => x.MessageId = messageId);
            await Task.Delay(50);

            await harness.Bus.Publish(new Command(), x => x.MessageId = messageId);
            await Task.Delay(5);

            await harness.InactivityTask;

            var count = await harness.Consumed.SelectAsync<Event>().Count();

            Assert.That(count, Is.EqualTo(100));
        }
    }


    public static class InboxLockInMemoryTestExtensions
    {
        public static IServiceCollection AddInboxLockInMemoryTestHarness(this IServiceCollection services)
        {
            services
                .AddInMemoryInboxOutbox()
                .AddMassTransitTestHarness(x =>
                {
                    x.AddHandler(async (Event message) => {});
                    x.AddConsumer<InboxLockConsumer, InboxLockInMemoryConsumerDefinition>();
                });

            services.AddOptions<TextWriterLoggerOptions>().Configure(options => options.Disable("Microsoft"));

            return services;
        }
    }


    namespace InboxLock
    {
        using System;
        using System.Linq;


        public class InboxLockConsumer :
            IConsumer<Command>
        {
            public async Task Consume(ConsumeContext<Command> context)
            {
                await Task.WhenAll(Enumerable.Range(0, 100).Select(index =>
                    context.Publish<Event>(new
                    {
                        context.MessageId,
                        Text = $"{index:0000}"
                    })));
            }
        }


        public class InboxLockInMemoryConsumerDefinition :
            ConsumerDefinition<InboxLockConsumer>
        {
            readonly IServiceProvider _provider;

            public InboxLockInMemoryConsumerDefinition(IServiceProvider provider)
            {
                _provider = provider;
            }

            protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
                IConsumerConfigurator<InboxLockConsumer> consumerConfigurator)
            {
                endpointConfigurator.UseMessageRetry(r => r.Intervals(10, 50, 100, 100, 100, 100, 100, 100));

                endpointConfigurator.UseInMemoryInboxOutbox(_provider);
            }
        }
    }
}
