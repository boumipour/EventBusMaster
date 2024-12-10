using MrEventBus.Abstraction.Models;

namespace MrEventBus.Abstraction.Subscriber;

public interface IMessageConsumer<TMessage> where TMessage : class
{
    public Task ConsumeAsync(MessageContext<TMessage> messeageContext);
}
