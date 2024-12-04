namespace MrEventBus.Abstraction.Consumer
{
    public interface IEventBusSubscriber
    {
        Task SubscribeAsync(CancellationToken cancellationToken = default);
    }
}
