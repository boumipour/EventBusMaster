using Microsoft.AspNetCore.Mvc;
using MrEventBus.Abstraction.Producer;

namespace TestApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly ILogger<EventsController> _logger;
        private readonly IEventBusProducer _publisher;

        public EventsController(IEventBusProducer publisher,ILogger<EventsController> logger)
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
