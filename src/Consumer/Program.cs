using Consumer;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddMassTransit(config =>
{
    config.AddConsumer<MessageConsumer>();

    config.AddBus(ctx => Bus.Factory.CreateUsingRabbitMq(cfg =>
        {
            cfg.Host(new Uri("amqps://pmarijuk:rhybldM32Hm7JiCIvnvbHD0i5ejhmWgK@gull.rmq.cloudamqp.com/pmarijuk"), h =>
            {
                h.Username("pmarijuk");
                h.Password("rhybldM32Hm7JiCIvnvbHD0i5ejhmWgK");
            });

            cfg.ReceiveEndpoint("Message", endpoint =>
            {
                endpoint.ConfigureConsumer<MessageConsumer>(ctx);
            });

            cfg.ReceiveEndpoint("Message2", endpoint =>
            {
                endpoint.ConfigureConsumer<MessageConsumer>(ctx);
            });
        })
    );
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
