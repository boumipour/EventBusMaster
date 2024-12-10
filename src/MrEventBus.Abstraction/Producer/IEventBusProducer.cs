namespace MrEventBus.Abstraction.Producer;

public interface IEventBusProducer
{
    Task PublishAsync<T>(T message, string shard = "", string queueName = "main") where T : class;
}
