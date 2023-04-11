namespace MassTransit.KafkaIntegration.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AvroContracts.AvroContracts;
    using Confluent.Kafka;
    using Confluent.Kafka.SyncOverAsync;
    using Confluent.SchemaRegistry;
    using Confluent.SchemaRegistry.Serdes;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using TestFramework;
    using Testing;


    public class Avro_Specs :
        InMemoryTestFixture
    {
        const string Topic = "avrogie";

        [Test]
        public async Task Should_produce()
        {
            static IAsyncSerializer<T> GetSerializer<T>(IServiceProvider provider)
            {
                return new AvroSerializer<T>(provider.GetService<ISchemaRegistryClient>());
            }

            static IDeserializer<T> GetDeserializer<T>(IServiceProvider provider)
            {
                return new AvroDeserializer<T>(provider.GetService<ISchemaRegistryClient>()).AsSyncOverAsync();
            }

            await using var provider = new ServiceCollection()
                .AddSingleton<ISchemaRegistryClient>(new CachedSchemaRegistryClient(new Dictionary<string, string>
                {
                    { "schema.registry.url", "localhost:8081" },
                }))
                .ConfigureKafkaTestOptions(options =>
                {
                    options.CreateTopicsIfNotExists = true;
                    options.TopicNames = new[] { Topic };
                    options.Partitions = 2;
                })
                .AddMassTransitTestHarness(x =>
                {
                    x.AddTaskCompletionSource<ConsumeContext<KafkaMessage>>();
                    x.AddRider(rider =>
                    {
                        rider.AddConsumer<TestKafkaMessageConsumer<KafkaMessage>>();

                        rider.AddProducer<string, KafkaMessage>(Topic, context => context.MessageId.ToString(), (context, cfg) =>
                        {
                            cfg.SetKeySerializer(GetSerializer<string>(context));
                            cfg.SetValueSerializer(GetSerializer<KafkaMessage>(context));
                        });

                        rider.UsingKafka((context, k) =>
                        {
                            k.TopicEndpoint<string, KafkaMessage>(Topic, nameof(Avro_Specs), c =>
                            {
                                c.AutoOffsetReset = AutoOffsetReset.Earliest;

                                c.SetKeyDeserializer(GetDeserializer<string>(context));
                                c.SetValueDeserializer(GetDeserializer<KafkaMessage>(context));

                                c.ConfigureConsumer<TestKafkaMessageConsumer<KafkaMessage>>(context);
                            });
                        });
                    });
                }).BuildServiceProvider();

            var harness = provider.GetTestHarness();

            await harness.Start();

            ITopicProducer<KafkaMessage> producer = harness.GetProducer<KafkaMessage>();

            var correlationId = NewId.NextGuid();
            var conversationId = NewId.NextGuid();
            var initiatorId = NewId.NextGuid();
            var messageId = NewId.NextGuid();
            await producer.Produce(new { Test = "text" }, Pipe.Execute<SendContext>(context =>
                {
                    context.CorrelationId = correlationId;
                    context.MessageId = messageId;
                    context.InitiatorId = initiatorId;
                    context.ConversationId = conversationId;
                }),
                harness.CancellationToken);

            var result = await provider.GetTask<ConsumeContext<KafkaMessage>>();

            Assert.AreEqual("text", result.Message.Test);
            Assert.That(result.SourceAddress, Is.EqualTo(new Uri("loopback://localhost/")));
            Assert.That(result.DestinationAddress, Is.EqualTo(new Uri($"loopback://localhost/{KafkaTopicAddress.PathPrefix}/{Topic}")));
            Assert.That(result.MessageId, Is.EqualTo(messageId));
            Assert.That(result.CorrelationId, Is.EqualTo(correlationId));
            Assert.That(result.InitiatorId, Is.EqualTo(initiatorId));
            Assert.That(result.ConversationId, Is.EqualTo(conversationId));
        }
    }


    public class Publishing_a_message_to_the_bus_through_the_outbox :
        InMemoryTestFixture
    {
        const string Topic = "outboxtobus";

        [Test]
        public async Task Should_use_the_default_endpoint_serializer()
        {
            static IAsyncSerializer<T> GetSerializer<T>(IServiceProvider provider)
            {
                return new AvroSerializer<T>(provider.GetService<ISchemaRegistryClient>());
            }

            static IDeserializer<T> GetDeserializer<T>(IServiceProvider provider)
            {
                return new AvroDeserializer<T>(provider.GetService<ISchemaRegistryClient>()).AsSyncOverAsync();
            }

            await using var provider = new ServiceCollection()
                .AddSingleton<ISchemaRegistryClient>(new CachedSchemaRegistryClient(new Dictionary<string, string>
                {
                    { "schema.registry.url", "localhost:8081" },
                }))
                .ConfigureKafkaTestOptions(options =>
                {
                    options.CreateTopicsIfNotExists = true;
                    options.TopicNames = new[] { Topic };
                    options.Partitions = 2;
                })
                .AddMassTransitTestHarness(x =>
                {
                    x.AddHandler(async (BusMessage _) =>
                    {
                    });

                    x.SetTestTimeouts(testInactivityTimeout: TimeSpan.FromSeconds(10));

                    x.AddRider(rider =>
                    {
                        rider.AddConsumer<MessageHandler>();

                        rider.AddProducer<string, KafkaMessage>(Topic, context => context.MessageId.ToString(), (context, cfg) =>
                        {
                            cfg.SetKeySerializer(GetSerializer<string>(context));
                            cfg.SetValueSerializer(GetSerializer<KafkaMessage>(context));
                        });

                        rider.AddInMemoryInboxOutbox();

                        rider.UsingKafka((context, k) =>
                        {
                            k.TopicEndpoint<string, KafkaMessage>(Topic, nameof(Avro_Specs), c =>
                            {
                                c.AutoOffsetReset = AutoOffsetReset.Earliest;

                                c.SetKeyDeserializer(GetDeserializer<string>(context));
                                c.SetValueDeserializer(GetDeserializer<KafkaMessage>(context));

                                c.UseInMemoryInboxOutbox(context);

                                c.ConfigureConsumer<MessageHandler>(context);
                            });
                        });
                    });
                }).BuildServiceProvider();

            var harness = provider.GetTestHarness();

            await harness.Start();

            ITopicProducer<KafkaMessage> producer = harness.GetProducer<KafkaMessage>();

            var correlationId = NewId.NextGuid();
            var conversationId = NewId.NextGuid();
            var initiatorId = NewId.NextGuid();
            var messageId = NewId.NextGuid();
            await producer.Produce(new { Test = "text" }, Pipe.Execute<SendContext>(context =>
                {
                    context.CorrelationId = correlationId;
                    context.MessageId = messageId;
                    context.InitiatorId = initiatorId;
                    context.ConversationId = conversationId;
                }),
                harness.CancellationToken);

            IReceivedMessage<BusMessage> message = await harness.Consumed.SelectAsync<BusMessage>().FirstOrDefault();

            Assert.That(message, Is.Not.Null);

            Assert.That(message.Context.Message.OriginalMessageId, Is.EqualTo(messageId));
            Assert.That(message.Context.Message.OriginalCorrelationId, Is.EqualTo(correlationId));
        }


        class MessageHandler :
            IConsumer<KafkaMessage>
        {
            public Task Consume(ConsumeContext<KafkaMessage> context)
            {
                return context.Publish(new BusMessage(context.MessageId, context.CorrelationId));
            }
        }


        record BusMessage(Guid? OriginalMessageId, Guid? OriginalCorrelationId);
    }
}
