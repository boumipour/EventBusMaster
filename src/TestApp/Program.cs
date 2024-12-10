using EventBus;
using MrEventBus.RabbitMQ.Configurations;
using TestApp;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMrEventBus(option => 
{
    option.ExchangeName = "test_app";
    option.HostName = "localhost";
    option.Port = 5672;
    option.VirtualHost = "/";
    option.UserName = "admin";
    option.Password = "admin";

    option.Producers = new[] { typeof(MyEvent) };
    option.Consumers = new[] { new Consumer()
    {
        ConsumerTypes=  new[] { typeof(MyEvent) },
        ExchangeName="test_app",
        QueueName="main"
    }};
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();


