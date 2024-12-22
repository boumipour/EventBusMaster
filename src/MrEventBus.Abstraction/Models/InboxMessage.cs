using System.Text.Json;

namespace MrEventBus.Abstraction.Models
{
    public enum InboxMessageState
    {
        None,
        Received = 1,
        InProgress = 2,
        Consumed = 3
    }

    public class InboxMessage
    {
        public Guid MessageId { get; set; }
        public string Type { get; set; }
        public string Data { get; set; }
        public string Shard { get; set; }
        public InboxMessageState State { get; set; }
        public string QueueName { get; set; }

        public DateTime PublishDateTime { get; set; }
        public DateTime CreateDateTime { get; set; }
        public DateTime LastModifyDateTime { get; set; }


        public InboxMessage()
        {
        }

        public InboxMessage(Guid id, object message, string shard, DateTime publishDateTime)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            MessageId = id;
            Data = JsonSerializer.Serialize(message);
            Type = message.GetType().FullName + ", " + message.GetType().Assembly.GetName();
            PublishDateTime = publishDateTime;
            CreateDateTime = DateTime.Now;
            LastModifyDateTime = DateTime.Now;
            State = InboxMessageState.Received;
            Shard = shard;
        }


        internal object RecreateMessage()
        {
            return JsonSerializer.Deserialize(Data, System.Type.GetType(Type)!);
        }

        internal void Consumed()
        {
            State = InboxMessageState.Consumed;
            LastModifyDateTime = DateTime.Now;
        }
    }
}
