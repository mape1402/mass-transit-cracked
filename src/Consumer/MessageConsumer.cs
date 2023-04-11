using MassTransit;
using MassTransit.Crack;

namespace Consumer
{
    public class MessageConsumer : IConsumer<Message>
    {
        public Task Consume(ConsumeContext<Message> context)
        {
            dynamic message = context.Message;

            Console.WriteLine(message.Text);
            return Task.CompletedTask;
        }
    }
}
