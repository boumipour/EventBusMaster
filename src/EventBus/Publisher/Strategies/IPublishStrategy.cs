using EventBus.Model;

namespace EventBus.Publisher.Strategies;

public interface IPublishStrategy
{
      Task PublishAsync<T>(MessageContext<T> messageContext, string queueName = "");
}
