namespace MrEventBus.RabbitMQ.Configurations;

public class RabbitMqConfiguration
{
    public string ExchangeName = "my_application";
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 2701;
    public string UserName { get; set; } = "admin";
    public string Password { get; set; } = "admin";
    public string VirtualHost { get; set; } = "MrEvebtBus";

    public int MaxConnectionPoolSize { get; set; } = 5;
    public IReadOnlyCollection<Type> Producers { get; set; } = new List<Type>();
    public IReadOnlyCollection<Consumer> Consumers { get; set; } = new List<Consumer>();

}

public class Consumer
{
    public string ExchangeName { get; set; }
    public string QueueName { get; set; }
    public IReadOnlyCollection<Type> ConsumerTypes { get; set; } = new List<Type>();
}
