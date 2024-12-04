using Microsoft.AspNetCore.Mvc;
using MrEventBus.Abstraction.Publisher;

namespace TestApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly ILogger<EventsController> _logger;
        private readonly IEventBusPublisher _publisher;

        public EventsController(IEventBusPublisher publisher,ILogger<EventsController> logger)
        {
            _logger = logger;
            _publisher = publisher;
        }

        [HttpPost(Name = "ProduceEvent")]
        public async Task<IActionResult> PostAsync()
        {
            MyEvent myEvent = new MyEvent();
            await _publisher.PublishAsync(myEvent);
            return Ok();
        }
    }
}
