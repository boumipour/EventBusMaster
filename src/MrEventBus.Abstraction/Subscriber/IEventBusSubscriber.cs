namespace MrEventBus.Abstraction.Subscriber;

public interface IEventBusSubscriber
{
    Task SubscribeAsync(CancellationToken cancellationToken = default);
}
