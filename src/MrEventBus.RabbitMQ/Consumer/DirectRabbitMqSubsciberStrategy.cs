using Microsoft.Extensions.Options;
using MrEventBus.Abstraction.Consumer.Strategies;
using MrEventBus.RabbitMQ.Configurations;
using MrEventBus.RabbitMQ.Infrastructures;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace MrEventBus.RabbitMQ.Consumer
{
    public class DirectRabbitMqSubsciberStrategy : ISubscribeStrategy
    {
        private readonly IRabbitMqConnectionManager _connectionManager;
        private readonly RabbitMqConfiguration _config;

        public DirectRabbitMqSubsciberStrategy(IRabbitMqConnectionManager connectionManager, IOptions<RabbitMqConfiguration> config)
        {
            _connectionManager = connectionManager;
            _config = config.Value;
        }

        public async Task SubscribeAsync(Func<string, Type, Task> messageRecieved, CancellationToken cancellationToken = default)
        {
            IEnumerable<string> exchanges = _config.Consumers.Select(s => s.ExchangeName.ToLower(System.Globalization.CultureInfo.CurrentCulture)).ToList();
            if (!exchanges.Any())
            {
                return;
            }

            var channel =await _connectionManager.GetChannelAsync();

            var listener = new AsyncEventingBasicConsumer(channel);

            listener.ReceivedAsync += (model, ea) =>
            {
                byte[] body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                return Task.CompletedTask;
            };


            // Start consuming
            foreach (var consumer in _config.Consumers)
            {
                await channel.BasicConsumeAsync(queue: $"{consumer.ExchangeName}.{consumer.QueueName}", autoAck: true, consumer: listener);
            }
           

        }
    }
}
