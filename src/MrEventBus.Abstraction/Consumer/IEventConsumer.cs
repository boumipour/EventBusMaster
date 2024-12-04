using MrEventBus.Abstraction.Models;

namespace MrEventBus.Abstraction.Consumer
{
    public interface IEventConsumer<TMessage> where TMessage : class
    {
        public Task ConsumeAsync(MessageContext<TMessage> messeageContext);
    }
}
