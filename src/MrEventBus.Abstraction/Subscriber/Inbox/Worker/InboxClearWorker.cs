using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MrEventBus.Abstraction.Subscriber.Inbox.Config;
using MrEventBus.Abstraction.Subscriber.Inbox.Repository;
using System.Diagnostics;

namespace MrEventBus.Abstraction.Subscriber.Inbox.Worker
{
    public class InboxClearWorker : BackgroundService
    {
        readonly InboxConfig _config;
        readonly ILogger<InboxClearWorker> _logger;
        readonly IInboxRepository _inboxRepository;

        public InboxClearWorker(IOptions<InboxConfig> config, ILogger<InboxClearWorker> logger, IInboxRepository inboxRepository)
        {
            _config = config.Value;
            _logger = logger;
            _inboxRepository = inboxRepository;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var _stopwatch = new Stopwatch();

            while (!stoppingToken.IsCancellationRequested)
            {
                _stopwatch.Restart();

                try
                {
                    await _inboxRepository.DeleteAsync(_config.PersistenceDuration.TotalDays);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    throw;
                }

                // max value should be 49 days or delay will throw OutOfRangeException
                if (_config.PersistenceDuration.TotalMilliseconds > uint.MaxValue)
                {
                    await Task.Delay(int.MaxValue, stoppingToken);
                }
                else
                {
                    await Task.Delay(_config.PersistenceDuration, stoppingToken);
                }
            }
        }
    }
}
