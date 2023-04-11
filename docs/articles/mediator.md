---
title: "Mediator"
---

# Enter the Mediator

This post covers a new MassTransit feature, _Mediator_, and how it can be used to consume messages from any transport using MassTransit consumers and sagas.

<!-- more -->

[Mediator](/usage/mediator), a new feature added in MassTransit v6.2, is a new way to use MassTransit. Mediator is entirely in-memory, does not require a transport, and does not serialize messages. Mediator sends messages directly to the receive pipeline, which then sends them to configured consumers, handlers, and sagas.

### Why mediator?

Mediator is a behavioral design pattern that reduces coupling using an intermediate layer that encapsulates the communication between objects. A MassTransit bus is a type of mediator, it supports sending and publishing messages to consumers, sagas, activities, and handlers that are decoupled from the message producer. _Mediator_ is another form, one that can be used in many of the same situations but without the overhead of message serialization and the distributed system complexity. By using _mediator_, the power of MassTransit is now available for a broader set of use cases with the same flexibility and programming model.


#### Kafka

Kafka support is a fairly common request. MassTransit is a bus, and it was designed to work with message brokers. A lot of people think Kafka is a message broker, but it isn't the type of broker that MassTransit expects. For instance, Kafka should not be used for RPC or a request-response conversation pattern such as a query. Kafka is designed as a streaming log writer, with topics that can have messages (each of which is a key-value pair of byte arrays). Messages are not delivered to consumers, they are read by consumers – similar to how records are read from a file.

So while Kafka has atoms like _topics_ and _messages_, those atoms are semantically different than those used by a typical message broker used with MassTransit.

However, it _would_ be pretty awesome to process messages read from a Kafka topic using MassTransit. And that's how _mediator_ started – a way to send any type (call it `T`) to the [receive pipeline](/advanced/middleware/receive) so that it can be consumed. And like any endeavor to add functionality, the same question, "_how hard can it be?_" Using _mediator_ to consume Kafka messages is now possible by sending the deserialized type using `await mediator.Send<T>(T message)`. 

> Note that [Kafka support is now built-in!](/usage/riders/kafka)

#### Speed

Mediator is fast. Even using the in-memory transport, MassTransit will serialize and deserialize messages, which adds considerable overhead. Mediator doesn't serialize, which means it isn't slow. Using the [MassTransit-Benchmark](https://github.com/MassTransit/MassTransit-Benchmark) with the `--mediator` option, send/consume is blazingly fast (over 650,000 messages/second on my 8-core Windows desktop), and request/response is pretty fast as well. Of course, these are fairly synthetic numbers – consumers will typically do more than just add a counter to a bucket.

### Using Mediator

To configure mediator in an ASP.NET Core project, add the following to the _ConfigureServices_ method.

> Packages used: [MassTransit](https://nuget.org/packages/MassTransit/), [MassTransit.Extensions.DependencyInjection](https://nuget.org/packages/MassTransit.Extensions.DependencyInjection/)

```cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddControllers();

    services.AddMediator(x =>
    {
        x.AddConsumersFromNamespaceContaining<OrderConsumer>();

        x.AddRequestClient<SubmitOrder>();
    });
}
```

Any consumers in the same namespace as the `OrderConsumer` will be added, along with any consumer definition classes found. The `AddMediator` configuration method is a `IReceiverEndpointConfigurator`, on which all of the consumers are configured. 

> The configuration can include all kinds of middleware, including popular favorites such as `UseMessageRetry` and `UseConcurrencyLimit`.

A request client is added for use by a controller where a response is expected. If no response is needed, call the `Send` method on the `IMediator` interface.

The consumer is a standard MassTransit consumer:

```cs
public class OrderConsumer :
    IConsumer<SubmitOrder>
{
    public Task Consume(ConsumeContext<SubmitOrder> context)
    {
        return context.RespondAsync(new OrderAccepted
        {
            Text = $"Received: {context.Message.OrderNumber} {DateTime.UtcNow}"
        });
    }
}
```

And the message contracts are simple classes (yes, interfaces can be used as well – and are still recommended).

```cs
public class SubmitOrder
{
    public int OrderNumber { get; set; }
}

public class OrderAccepted
{
    public string Text { get; set; }
}
```

The controller method uses the request client to communicate (indirectly, via the mediator) with the `OrderConsumer`.

```cs
[Route("/orders")]
public class OrderController : 
    Controller
{
    readonly IRequestClient<SubmitOrder> _requestClient;

    public OrderController(IRequestClient<SubmitOrder> requestClient)
    {
        _requestClient = requestClient;
    }

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] OrderDto order, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _requestClient.GetResponse<OrderAccepted>(new { OrderNumber = order.ON }, cancellationToken);

            return Content($"Order Accepted 123: {response.Message.Text}");
        }
        catch (RequestTimeoutException)
        {
            return StatusCode((int)HttpStatusCode.RequestTimeout);
        }
        catch (Exception)
        {
            return StatusCode((int)HttpStatusCode.RequestTimeout);
        }
    }
}
```

### What about Kafka?

::: tip UPDATE
[Kafka support is now built-in!](/usage/riders/kafka) _This section is retained for historical purposes, but is no longer recommended_
:::

Using the Confluent Kafka client, AVRO, and the schema registry – it is possible to send messages to _mediator_.

```cs
CancellationTokenSource cts = new CancellationTokenSource();
var consumeTask = Task.Run(() =>
{
    using var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig);
    using var consumer = new ConsumerBuilder<string, OrderUpdate>(consumerConfig)
            .SetKeyDeserializer(new AvroDeserializer<string>(schemaRegistry).AsSyncOverAsync())
            .SetValueDeserializer(new AvroDeserializer<OrderUpdate>(schemaRegistry).AsSyncOverAsync())
            .SetErrorHandler((_, e) => Console.WriteLine($"Error: {e.Reason}"))
            .Build());

    consumer.Subscribe("order-updates");

    try
    {
        while (true)
        {
            try
            {
                var consumeResult = consumer.Consume(cts.Token);

                await mediator.Send(consumeResult.Message, cts.Token);
            }
            catch (ConsumeException e)
            {
                Console.WriteLine($"Consume error: {e.Error.Reason}");
            }
        }
    }
    catch (OperationCanceledException)
    {
        consumer.Close();
    }
});
```

This is just an example, based off a sample from the Confluent site.




