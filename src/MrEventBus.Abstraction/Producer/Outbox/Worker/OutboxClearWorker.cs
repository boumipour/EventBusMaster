using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MrEventBus.Abstraction.Producer.Outbox.Config;
using MrEventBus.Abstraction.Producer.Outbox.Repository;
using System.Diagnostics;

namespace MrEventBus.Abstraction.Producer.Outbox.Worker;

public class OutboxClearWorker : BackgroundService
{
    readonly OutboxConfiguration _config;
    readonly IOutboxRepository _outboxRepository;

    public OutboxClearWorker(IOptions<OutboxConfiguration> config, IOutboxRepository outboxRepository)
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

                await _outboxRepository.DeleteAsync(_config.OutboxPersistenceDuration.TotalDays);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }

            ///max value should be 49 days or delay will throw OutOfRangeException
            if (_config.OutboxPersistenceDuration.TotalMilliseconds > uint.MaxValue)
            {
                await Task.Delay(int.MaxValue, stoppingToken);
            }
            else
            {
                await Task.Delay(_config.OutboxPersistenceDuration, stoppingToken);
            }
        }
    }
}

