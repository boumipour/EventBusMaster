using MrEventBus.Abstraction.Models;
using MrEventBus.Abstraction.Subscriber;

namespace TestApp
{
    public class MyEventConsumer : IMessageConsumer<MyEventConsumer>
    {
        public Task ConsumeAsync(MessageContext<MyEventConsumer> messeageContext)
        {
            Console.WriteLine( messeageContext.Message);
            return Task.CompletedTask;
        }
    }
}
