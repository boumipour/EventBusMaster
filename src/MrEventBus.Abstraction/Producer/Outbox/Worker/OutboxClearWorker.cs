using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MrEventBus.Abstraction.Producer.Outbox.Config;
using MrEventBus.Abstraction.Producer.Outbox.Repository;
using System.Diagnostics;

namespace MrEventBus.Abstraction.Producer.Outbox.Worker;

public class OutboxClearWorker : BackgroundService
{
    readonly OutboxConfig _config;
    readonly IOutboxRepository _outboxRepository;

    public OutboxClearWorker(IOptions<OutboxConfig> config, IOutboxRepository outboxRepository)
    {
        _config = config.Value;
        _outboxRepository = outboxRepository;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var _stopwatch = new Stopwatch();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _stopwatch.Restart();

                await _outboxRepository.DeleteAsync(_config.PersistenceDuration.TotalDays);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }

            ///max value should be 49 days or delay will throw OutOfRangeException
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

