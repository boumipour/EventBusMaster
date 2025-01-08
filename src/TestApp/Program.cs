using EventBus;
using MrEventBus.Abstraction.Subscriber;
using MrEventBus.RabbitMQ.Configurations;
using MrEventBus.Storage.MySql;
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

    option.PoolSizePerQueue = 1;

    option.Producers = new[] { typeof(MyEvent) };
    option.Consumers = new[] { new Consumer()
    {
        ConsumerTypes=  new[] { typeof(MyEvent) },
        ExchangeName="test_app",
        ConcurrencyLevel=1,
        PrefetchCount=1,
        QueueName="main"
    }};
})
.AddMySqlOutBoxing(option =>
{
    option.EnabledProcessor = true;
    option.Concurrency = 1;
    option.ReaderInterval = new TimeSpan(0, 0, 1);
    option.EnabledEvents = new[] { typeof(MyEvent) };

    option.MySqlConnectionString = "Server=localhost;User ID=root;Password=root;Database=db;Allow User Variables=true;";
    option.DBInitializer = true;
})
.AddMySqlInBoxing(option =>
{
    option.EnabledProcessor = true;
    option.Concurrency = 1;
    option.ReaderInterval = new TimeSpan(0, 0, 1);
    option.EnabledEvents = new[] { typeof(MyEvent) };

    option.DBInitializer = true;
    option.MySqlConnectionString = "Server=localhost;User ID=root;Password=root;Database=db;Allow User Variables=true;";
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


