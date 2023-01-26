using System.Threading.Tasks;
using System.Threading;
using EIS.Api.Application.Common.Interfaces;
using EIS.Api.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using EIS.Api.Application.Constants;

namespace EIS.Api.Application.Publisher;

public class ItemCreatedEventDispatcher : INotificationHandler<DomainEventNotification<ItemCreatedEvent>>
{
    private readonly ILogger<ItemCreatedEventDispatcher> _logger;
    private readonly IDomainEventDispatcher<ItemManager> _domainEventDispatcher;

    public ItemCreatedEventDispatcher(ILogger<ItemCreatedEventDispatcher> logger, IDomainEventDispatcher<ItemManager> domainEventDispatcher) 
    {
        _logger = logger;
        _domainEventDispatcher = domainEventDispatcher;
    }

    public async Task Handle(DomainEventNotification<ItemCreatedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        await _domainEventDispatcher.DispatchEvent(domainEvent., EISEventTypes.ItemManagement.ITEM_CREATED);
        _logger.LogInformation("Item created event published");
    }
}

public class ItemCreatedDispatcher : IDomainEventDispatcher<ItemManager>
{
    private readonly IEventPublisherService _eventDispatcherService;

    public ItemCreatedDispatcher(IEventPublisherService eventDispatcherService)
    {
        _eventDispatcherService = eventDispatcherService;
    }

    public async Task DispatchEvent(ItemMaster itemMaster, string eventType)
    {
        if (EISConstants.PublishStatus)
        {
            var ItemCreatedContract = new ItemCreatedContract
            {

            };

            Payload itemCreatedPayload = new (ItemCreatedContract, "ItemCreated", "Item-Management");
            EisEventPayloadBehavior eisItemCreatedPayloadBehaviour = new(itemCreatedPayload, eventType);

            await _eventDispatcherService.Publish()
        }

        await Task.Completed;
    }

}