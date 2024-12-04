namespace MrEventBus.Abstraction.Consumer.Strategies
{
    public  interface ISubscribeStrategy
    {
        Task SubscribeAsync(Func<string, Type, Task> messageRecieved, CancellationToken cancellationToken = default);
    }
}
