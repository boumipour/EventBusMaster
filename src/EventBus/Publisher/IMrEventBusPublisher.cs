namespace EventBus.Publisher;

public interface IMrEventBusPublisher
{
      Task PublishAsync<T>(T message, string shard = "", string queueName = "") where T : class;
}
