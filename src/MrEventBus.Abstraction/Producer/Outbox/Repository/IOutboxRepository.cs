using MrEventBus.Abstraction.Models;

namespace MrEventBus.Abstraction.Producer.Outbox.Repository;

public interface IOutboxRepository
{
    Task<IEnumerable<OutboxMessage>> GetAsync();
    Task CreateAsync(OutboxMessage outboxMessage);
    Task UpdateAsync(OutboxMessage outboxMessage);
    Task DeleteAsync(double persistencePeriodInDays);
}
