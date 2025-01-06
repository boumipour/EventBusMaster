using EventBus;
using MrEventBus.Abstraction.Subscriber;
using MrEventBus.Box.MySql;
using MrEventBus.RabbitMQ.Configurations;
using TestApp;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    ;
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

    option.PoolSizePerQueue = 2;

    option.Producers = new[] { typeof(MyEvent) };
    option.Consumers = new[] { new Consumer()
    {
        ConsumerTypes=  new[] { typeof(MyEvent) },
        ExchangeName="test_app",
        ConcurrencyLevel=10,
        PrefetchCount=10,
        QueueName="main"
    }};
})
.AddMySqlOutBoxing(option =>
{
    option.EnabledProcessor = true;
    option.Concurrency = 2;
    option.ReaderInterval = new TimeSpan(0, 0, 1);
    option.EnabledEvents = new[] { typeof(MyEvent) };

    option.MySqlConnectionString = "Server=localhost;User ID=root;Password=root;Database=db;";
    option.DBInitializer = false;
})
.AddMySqlInBoxing(option =>
{
    option.EnabledProcessor = true;
    option.Concurrency = 2;
    option.ReaderInterval = new TimeSpan(0, 0, 1);
    option.EnabledEvents = new[] { typeof(MyEvent) };

    option.DBInitializer = false;
    option.MySqlConnectionString = "Server=localhost;User ID=root;Password=root;Database=db;";
});


builder.Services.AddScoped<IMessageConsumer<MyEvent>, MyEventConsumer>();

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


