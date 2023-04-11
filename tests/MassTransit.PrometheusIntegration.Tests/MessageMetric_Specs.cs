namespace MassTransit.PrometheusIntegration.Tests
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Prometheus;
    using TestFramework;
    using TestFramework.Messages;


    [TestFixture]
    public class MessageMetric_Specs :
        InMemoryTestFixture
    {
        [Test]
        public async Task Should_capture_the_bus_instance_metric()
        {
            await InputQueueSendEndpoint.Send(new PingMessage());
            await InputQueueSendEndpoint.Send(new PingMessage());
            await InputQueueSendEndpoint.Send(new PingMessage());
            await InputQueueSendEndpoint.Send(new PingMessage());
            await InputQueueSendEndpoint.Send(new PingMessage());

            await Bus.Publish(new PingMessage());
            await Bus.Publish(new PingMessage());
            await Bus.Publish(new PingMessage());

            await Bus.Publish<GenericMessage<LongMessage>>(new { Message = new LongMessage() });

            await InactivityTask;

            using var stream = new MemoryStream();
            await Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream);

            var text = Encoding.UTF8.GetString(stream.ToArray());

            Console.WriteLine(text);

            Assert.That(text.Contains("mt_publish_total{service_name=\"unit_test\",message_type=\"PingMessage\"} 3"), "publish");
            Assert.That(text.Contains("mt_send_total{service_name=\"unit_test\",message_type=\"PingMessage\"} 5"), "send");
            Assert.That(text.Contains("mt_receive_total{service_name=\"unit_test\",endpoint_address=\"input_queue\"} 9"), "receive");
            Assert.That(text.Contains("mt_consume_total{service_name=\"unit_test\",message_type=\"PingMessage\",consumer_type=\"TestConsumer\"} 8"), "consume");
        }

        protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
            configurator.UsePrometheusMetrics(serviceName: "unit_test");
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            configurator.Consumer(() => new TestConsumer());
            configurator.Consumer(() => new GenericConsumer<GenericMessage<LongMessage>>());
        }
    }


    public class LongMessage
    {
    }


    public interface GenericMessage<out T>
    {
        T Message { get; }
    }


    public class GenericConsumer<T> :
        IConsumer<T>
        where T : class
    {
        public Task Consume(ConsumeContext<T> context)
        {
            return Task.CompletedTask;
        }
    }


    public class TestConsumer :
        IConsumer<PingMessage>
    {
        public Task Consume(ConsumeContext<PingMessage> context)
        {
            return Task.CompletedTask;
        }
    }
}
