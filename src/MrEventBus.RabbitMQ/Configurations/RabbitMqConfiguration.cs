namespace MrEventBus.RabbitMQ.Configurations;

public class RabbitMqConfiguration
{
    public string ExchangeName = "my_application";
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 2701;
    public string UserName { get; set; } = "admin";
    public string Password { get; set; } = "admin";
    public string VirtualHost { get; set; } = "MrEvebtBus";

    
    public int MinChannelPoolSize { get; set; } = 5;
    public int MaxChannelPoolSize { get; set; } = 20;
    public int MaxDynamicChannelPoolSize { get; set; } = 80;
    public TimeSpan ChannelPoolCleanupInterval { get; set; } = new TimeSpan(0, 1, 0);


    public int MaxConnectionPoolSize { get; set; } = 5;
    public IReadOnlyCollection<Type> Producers { get; set; } = new List<Type>();
    public IReadOnlyCollection<Consumer> Consumers { get; set; } = new List<Consumer>();

}

public class Consumer
{
    public required string ExchangeName { get; set; }
    public required string QueueName { get; set; }
    public int ConcurrencyLevel { get; set; } = 1;
    public ushort PrefetchCount { get; set; } = 1;
    public IReadOnlyCollection<Type> ConsumerTypes { get; set; } = new List<Type>();
}
