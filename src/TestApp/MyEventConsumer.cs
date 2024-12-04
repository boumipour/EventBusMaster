using MrEventBus.Abstraction.Consumer;
using MrEventBus.Abstraction.Models;

namespace TestApp
{
    public class MyEventConsumer : IEventConsumer<MyEventConsumer>
    {
        public Task ConsumeAsync(MessageContext<MyEventConsumer> messeageContext)
        {
            Console.WriteLine( messeageContext.Message);
            return Task.CompletedTask;
        }
    }
}
