using System.Text.Json;

namespace MrEventBus.Abstraction.Models
{
    public enum OutboxMessageState
    {
        None,
        ReadyToSend = 1,
        InProgress = 2,
        Sended = 3
    }

    public class OutboxMessage
    {
        public Guid MessageId { get; set; }
        public string Type { get; set; }
        public string Data { get; set; }
        public string Shard { get; set; }
        public string QueueName { get; set; }
        public OutboxMessageState State { get; set; }
        public DateTime CreateDateTime { get; set; }
        public DateTime LastModifyDateTime { get; set; }

        public OutboxMessage()
        {
        }

        public OutboxMessage(Guid id, object message, string shard = "", string queueName = "main")
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            MessageId = id;
            Data = JsonSerializer.Serialize(message);
            Type = message.GetType().FullName + ", " + message.GetType().Assembly.GetName();
            CreateDateTime = DateTime.Now;
            LastModifyDateTime = DateTime.Now;
            State = OutboxMessageState.ReadyToSend;
            Shard = shard;
            QueueName = queueName;
        }

        internal void DeliveredToBus()
        {
            State = OutboxMessageState.Sended;
            LastModifyDateTime = DateTime.Now;
        }

        internal object RecreateMessage()
        {
            return JsonSerializer.Deserialize(Data, System.Type.GetType(Type)!);
        }
    }
}
