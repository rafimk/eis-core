namespace EIS.Api.Application.Publisher
{
    public record ItemCreatedEvent(Guid Id, string ItemName, DateTime Created);
}
