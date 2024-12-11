using MrEventBus.Abstraction.Models;
using MrEventBus.Abstraction.Subscriber;

namespace TestApp
{
    public class MyEventConsumer : IMessageConsumer<MyEvent>
    {
        public MyEventConsumer()
        {
            
        }

        public Task ConsumeAsync(MessageContext<MyEvent> messeageContext)
        {
            Console.WriteLine( messeageContext.Message);
            return Task.CompletedTask;
        }
    }
}
