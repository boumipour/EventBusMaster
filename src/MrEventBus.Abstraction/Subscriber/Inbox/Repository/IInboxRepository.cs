using MrEventBus.Abstraction.Models;

namespace MrEventBus.Abstraction.Subscriber.Inbox.Repository
{
    public interface IInboxRepository
    {
        Task<InboxMessage> GetAsync(Guid messageId);
        Task<IEnumerable<InboxMessage>> GetAsync();
        Task CreateAsync(InboxMessage inboxMessage);
        Task UpdateAsync(InboxMessage inboxMessage);
        Task DeleteAsync(double persistencePeriodInDays);
    }
}
