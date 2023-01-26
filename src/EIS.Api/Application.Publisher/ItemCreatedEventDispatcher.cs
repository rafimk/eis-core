using System.Threading.Tasks;
using System.Threading;
using EIS.Api.Application.Common.Interfaces;
using EIS.Api.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using EIS.Api.Application.Constants;
using EIS.Application.Interfaces;
using EIS.Api.Application.Contrats;
using EIS.Domain.Entities;

namespace EIS.Api.Application.Publisher;

public class ItemCreatedEventDispatcher : INotificationHandler<DomainEventNotification<ItemCreatedEvent>>
{
    private readonly ILogger<ItemCreatedEventDispatcher> _logger;
    private readonly IDomainEventDispatcher<ItemMaster> _domainEventDispatcher;

    public ItemCreatedEventDispatcher(ILogger<ItemCreatedEventDispatcher> logger, IDomainEventDispatcher<ItemMaster> domainEventDispatcher) 
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

public class ItemCreatedDispatcher : IDomainEventDispatcher<ItemMaster>
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
            var temCreatedContract = new ItemCreatedContract(itemMaster.Id, itemMaster.ItemName, itemMaster.Created);

            Payload itemCreatedPayload = new Payload(ItemCreatedContract, "ItemCreated", "Item-Management");
            EisEventPayloadBehavior eisItemCreatedPayloadBehaviour = new(itemCreatedPayload, eventType);

            await _eventDispatcherService.Publish();
        }

        await Task.Completed;
    }

}