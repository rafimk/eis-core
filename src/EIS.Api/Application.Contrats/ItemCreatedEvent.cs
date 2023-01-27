namespace EIS.Api.Application.Contrats
{
    public record ItemCreated(Guid Id, string ItemName, DateTime Created);

    public class ItemCreatedEvent : DomainEvent
    {
        public ItemCreated Item { get; }

        public ItemCreatedEvent(ItemCreated item)
        {
            Item = item;
        }
    }
}
