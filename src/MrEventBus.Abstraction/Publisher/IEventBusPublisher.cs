namespace MrEventBus.Abstraction.Publisher;

public interface IEventBusPublisher
{
    Task PublishAsync<T>(T message, string shard = "", string queueName = "") where T : class;
}
