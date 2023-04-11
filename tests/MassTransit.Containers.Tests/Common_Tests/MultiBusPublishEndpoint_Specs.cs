namespace MassTransit.Containers.Tests.Common_Tests
{
    using System;
    using System.Threading.Tasks;
    using DependencyInjection;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using Testing;


    public class MultiBusPublishEndpoint_Specs
    {
        [Test]
        public async Task Should_resolve_the_correct_bus()
        {
            await using var provider = new ServiceCollection()
                .AddMassTransitTestHarness()
                .AddMassTransit<IFirstBus>(busConfigurator =>
                {
                    busConfigurator.AddConsumer<SomeConsumer>();
                    busConfigurator.UsingInMemory((context, cfg) =>
                    {
                        cfg.Host(new Uri("loopback://localhost/first"));
                        cfg.ConfigureEndpoints(context);
                    });
                })
                .AddScoped<SomeService>()
                .AddMassTransit<ISecondBus>(busConfigurator =>
                {
                    busConfigurator.UsingInMemory((context, cfg) =>
                    {
                        cfg.Host(new Uri("loopback://localhost/second"));
                        cfg.ConfigureEndpoints(context);
                    });
                }).BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            var service = harness.Scope.ServiceProvider.GetRequiredService<SomeService>();

            await service.Process(new SomeEventReceived());

            var firstBus = provider.GetRequiredService<IFirstBus>();

            await firstBus.Publish(new SomeEventReceived());

            await Task.Delay(1000);

            await harness.Stop();
        }


        public interface ISecondBus :
            IBus
        {
        }


        public interface IFirstBus :
            IBus
        {
        }


        class SomeConsumer :
            IConsumer<SomeEventReceived>
        {
            readonly SomeService _someService;

            public SomeConsumer(SomeService someService)
            {
                _someService = someService;
            }

            public async Task Consume(ConsumeContext<SomeEventReceived> context)
            {
                await _someService.Process(context.Message);
            }
        }


        public class SomeService
        {
            readonly IPublishEndpoint _publishEndpoint;

            public SomeService(Bind<ISecondBus, IPublishEndpoint> publishEndpoint)
            {
                _publishEndpoint = publishEndpoint.Value;
            }

            public async Task Process(SomeEventReceived _)
            {
                await _publishEndpoint.Publish(new SomeEvent());
            }
        }
    }


    public class SomeEventReceived
    {
    }


    public class SomeEvent
    {
    }
}
