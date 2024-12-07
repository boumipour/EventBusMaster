namespace MrEventBus.Abstraction.Publisher;

public interface IEventBusPublisher
{
    Task PublishAsync<T>(T message, string shard = "", string queueName = "main") where T : class;
}
