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
using EIS.Api.Domain.Common;
using EIS.Api.Application.Common.Behaviour;

namespace EIS.Api.Application.Publisher;

public class ItemCreatedEventDispatcher : INotificationHandler<DomainEventNotification<ItemCreatedEvent>>
{
    private readonly ILogger<ItemCreatedEventDispatcher> _logger;
    private readonly IDomainEventDispatcher<ItemCreated> _domainEventDispatcher;

    public ItemCreatedEventDispatcher(ILogger<ItemCreatedEventDispatcher> logger, IDomainEventDispatcher<ItemCreated> domainEventDispatcher) 
    {
        _logger = logger;
        _domainEventDispatcher = domainEventDispatcher;
    }

    public async Task Handle(DomainEventNotification<ItemCreatedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        await _domainEventDispatcher.DispatchEvent(domainEvent.Item, EISEventTypes.ItemManagement.ITEM_CREATED);
        _logger.LogInformation("Item created event published");
    }
}

public class ItemCreatedDispatcher : IDomainEventDispatcher<ItemCreated>
{
    private readonly IEventPublisherService _eventDispatcherService;

    public ItemCreatedDispatcher(IEventPublisherService eventDispatcherService)
    {
        _eventDispatcherService = eventDispatcherService;
    }

    public async Task DispatchEvent(ItemCreated itemCreated, string eventType)
    {
        if (EISConstants.PublishStatus)
        {
            ItemCreatedContract itemCreatedContract = new ItemCreatedContract
            {
                Id = itemCreated.Id,
                ItemName = itemCreated.ItemName,
                Created = itemCreated.Created
            };

            Payload itemCreatedPayload = new Payload(itemCreatedContract, "ItemCreated", "Item-Management");
            EisEventPayloadBehaviour eisItemCreatedPayloadBehaviour = new(itemCreatedPayload, eventType);

            await _eventDispatcherService.Publish(eisItemCreatedPayloadBehaviour);
        }

        await Task.CompletedTask;
    }

}