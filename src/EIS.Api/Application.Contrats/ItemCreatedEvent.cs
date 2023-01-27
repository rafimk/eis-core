namespace EIS.Api.Application.Contrats
{
    public record ItemCreatedContract(Guid Id, string ItemName, DateTime Created);

    public class ItemCreatedEvent : DomainEvent
    {
        public ItemCreatedContract Item { get; }

        public ItemCreatedEvent(ItemCreatedContract item)
        {
            Item = item;
        }
    }
}
