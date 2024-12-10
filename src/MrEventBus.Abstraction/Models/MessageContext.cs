namespace MrEventBus.Abstraction.Models;

public class MessageContext<T>
{
    public Guid MessageId { get; set; }
    public required T Message { get; set; }
    public string Shard { get; set; } = string.Empty;
    public DateTime PublishDateTime { get; set; }
}
