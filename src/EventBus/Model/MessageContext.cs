namespace EventBus.Model;

public class MessageContext<T>
    {
        public Guid MessageId { get; set; }
        public T Message { get; set; }
        public string Shard { get; set; }
        public DateTime PublishDateTime { get; set; }
    }
