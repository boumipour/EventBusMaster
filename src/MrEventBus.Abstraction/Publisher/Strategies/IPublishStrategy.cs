using MrEventBus.Abstraction.Models;

namespace MrEventBus.Abstraction.Publisher.Strategies;

public interface IPublishStrategy
{
    Task PublishAsync<T>(MessageContext<T> messageContext, string queueName = "");
}
