# Kafka

Kafka is supported as a [Rider](/usage/riders/), and supports consuming and producing messages from/to Kafka topics. The Confluent .NET client is used, and has been tested with the community edition (running in Docker).

### Topic Endpoints

> Uses [MassTransit.RabbitMQ](https://nuget.org/packages/MassTransit.RabbitMQ/), [MassTransit.Kafka](https://nuget.org/packages/MassTransit.Kafka/), [MassTransit.Extensions.DependencyInjection](https://www.nuget.org/packages/MassTransit.Extensions.DependencyInjection/)

> Note: the following examples are using the RabbitMQ Transport. You can also use InMemory Transport to achieve the same effect when developing. With that, there is no need to install MassTransit.RabbitMQ.
> `x.UsingInMemory((context,config) => config.ConfigureEndpoints(context));`


To consume a Kafka topic, configure a Rider within the bus configuration as shown.

<<< @/docs/code/riders/KafkaConsumer.cs

A _TopicEndpoint_ connects a Kafka Consumer to a topic, using the specified topic name. The consumer group specified should be unique to the application, and shared by a cluster of service instances for load balancing (but it is possible to consume messages from multiple groups using separate endpoints). Consumers and sagas can be configured on the topic endpoint, which should be registered in the rider configuration. While the configuration for topic endpoints is the same as a receive endpoint, there is no implicit binding of consumer message types to Kafka topics. The message type is specified on the TopicEndpoint as a generic argument.

#### Wildcard support

Kafka allows to subscribe to multiple topics by using Regex (also called wildcard) which matches multiple topics:

<<< @/docs/code/riders/KafkaWildcardConsumer.cs

### Configuration

The configuration includes through [Confluent](https://docs.confluent.io/kafka-clients/dotnet/current/overview.html) client configs or using configurators to overrides it with style.

#### Checkpoint

Rider implementation is taking full responsibility of Checkpointing, there is no ability to change it.
Checkpointer can be configured on topic bases through next properties:

| Name                   | Description                                           | Default |
|:-----------------------|:------------------------------------------------------|:-----|
| CheckpointInterval     | Checkpoint frequency based on time                    | 1 min
| CheckpointMessageCount | Checkpoint every X messages                           | 5000
| MessageLimit           | Checkpointer buffer size without blocking consumption | 10000

> Please note, each topic partition has it's own checkpointer and configuration is applied to partition and not to entire topic.

During graceful shutdown Checkpointer will try to "checkpoint" all already consumed messages. Force shutdown should be avoided to prevent multiple consumption for the same message.

#### Scalability
Riders are designed with performance in mind, handling each topic partition withing separate threadpool. As well, allowing to scale-up consumption within same partition by using Key, as long as keys are different they will be processed concurrently and all this **without** sacrificing ordering.

| Name                    | Description                                                                                                                                                                      | Default |
|:------------------------|:---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|:-----|
| ConcurrentConsumerLimit | Number of Confluent Consumer instances withing same endpoint                                                                                                                     | 1
| ConcurrentDeliveryLimit | Number of Messages delivered concurrently within same partition + key. Increasing this value will **break ordering**, helpful for topics where ordering is not required          | 1
| ConcurrentMessageLimit  | Number of Messages processed concurrently witin different keys (preserving ordering). When keys are the same for entire partition `ConcurrentDeliveryLimit` will be used instead | 1
| PrefetchCount           | Number of Messages to prefetch from kafka topic into memory                                                                                                                      | 1000

::: warning
`ConcurrentConsumerLimit` is very powerful setting as Confluent consumer is reading one partition at a time, this will allow creating multiple consumers to read from separate partitions. But having higher number of Consumers than Number of Total Partitions would result of having **idle** consumers
:::

#### Configure Topology
::: warning
Kafka is not intended to create topology during startup. Topics should be created with correct number of partitions and replicas beforehand
:::

When client has *required* permissions and `CreateIfMissing` is configured, topic can be created on startup 

<<< @/docs/code/riders/KafkaTopicTopology.cs

### Producers

Producing messages to Kafka topics requires the producer to be registered. The producer can then be used to produce messages to the specified Kafka topic. In the example below, messages are produced to the Kafka topic as they are entered by the user.

<<< @/docs/code/riders/KafkaProducer.cs

#### Tombstone message

A record with the same key from the record we want to delete is produced to the same topic and partition with a null payload. These records are called tombstones.
This could be done by setting custom value serializer during produce:

<<< @/docs/code/riders/KafkaTombstoneProducer.cs

> Note, `null` message is not possible to consume and will be always skipped

### Producing and Consuming Multiple Message Types on a Single Topic

There are situations where you might want to produce / consume events of different types on the same Kafka topic. A common use case is to use a single topic to log ordered meaningful state change events like `SomethingRequested`, `SomethingStarted`, `SomethingFinished`.

Confluent have some documentation about how this can be implemented on the Schema Registry side:

- [Confluent Docs - Multiple Event Types in the Same Topic](https://docs.confluent.io/platform/current/schema-registry/serdes-develop/index.html#multiple-event-types-in-the-same-topic)
- [Confluent Docs - Multiple Event Types in the Same Topic with Avro](https://docs.confluent.io/platform/current/schema-registry/serdes-develop/serdes-avro.html#multiple-event-types-in-the-same-topic)
- [Confluent Blog - Multiple Event Types in the Same Topic](https://www.confluent.io/blog/multiple-event-types-in-the-same-kafka-topic/)

Unfortunately, it is [not yet widely supported in client tools and products](https://docs.confluent.io/platform/current/schema-registry/serdes-develop/index.html#limitations) and there is limited documentation about how to support this in your own applications. 

However, it is possible... The following demo uses the MassTransit Kafka Rider with custom [Avro](https://avro.apache.org/docs/current/) serializer / deserializer implementations and the Schema Registry to support multiple event types on a single topic:

[MassTransit-Kafka-Demo](https://github.com/danmalcolm/masstransit-kafka-demo)

The custom serializers / deserializer implementations leverage the wire format used by the standard Confluent schema-based serializers, which includes the schema id in the data stored for each message. This is also good news for interoperability with non-MassTransit applications.

**Warning: It's a little hacky and only supports the Avro format, but there's enough there to get you started.**
