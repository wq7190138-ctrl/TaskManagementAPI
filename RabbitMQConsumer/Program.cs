using RabbitMQConsumer;
using RabbitMQConsumer.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<TaskCreatedConsumerService>();

var host = builder.Build();
host.Run();
