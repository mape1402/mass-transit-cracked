using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Producer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RabbitMQController : ControllerBase
    {
        private readonly IPublishEndpoint _publishEndpoint;

        public RabbitMQController(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }

        [HttpPost]
        public async Task<ActionResult> Post()
        {
            await _publishEndpoint.Publish(new Message { Text = "Hello" });
            return Ok();
        }
    }
}
