namespace SchedulingAmazonSQS;

using MassTransit;
using Microsoft.Extensions.DependencyInjection;

public class Program
{
    public static void Main()
    {
        var services = new ServiceCollection();

        services.AddMassTransit(x =>
        {
            x.AddDelayedMessageScheduler();

            x.UsingAmazonSqs((context, cfg) =>
            {
                cfg.UseDelayedMessageScheduler();

                cfg.ConfigureEndpoints(context);
            });
        });
    }
}
