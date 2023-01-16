using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Net;
using System;
using System.ComponentModel;
using System.Diagnostics.Tracing;
using EIS.Application.Interfaces;
using Microsoft.Extensions.Logging;
using EIS.Infrastructure.Configuration;
using EIS.Application.Util;
using System.Threading.Tasks;
using EIS.Application.Constants;
using EIS.Domain.Entities;
using System.Collections.Generic;

namespace EIS.Infrastructure;

public class MessageQueueManager : IMessageQueueManager
{
    private readonly IBrockerConnectionFactory _brokerConnectionFactory;
    private readonly IEventInboxOutboxDbContext _eventInboxOutboxDbContext;
    private readonly ICompetingConsumerDbContext _competingConsumerDbContext;
    private readonly IConfigurationManager _configManager;
    private readonly ILogger<MessageQueueManager> _log;
    private readonly EventHandlerRegistry _eventRegistry;
    private readonly string eisHostIp;
    private readonly string _OutboundTopic;
    private readonly string _InboundQueue;

    public MessageQueueManager(IBrockerConnectionFactory brokerConnectionFactory, 
            IEventInboxOutboxDbContext eventInboxOutboxDbContext,
            ICompetingConsumerDbContext competingConsumerDbContext,
            IConfigurationManager configManager,
            ILogger<MessageQueueManager> log, 
            EventHandlerRegistry eventRegistry
            )
    {
        _brokerConnectionFactory = brokerConnectionFactory;
        _eventInboxOutboxDbContext = eventInboxOutboxDbContext;
        _competingConsumerDbContext = competingConsumerDbContext;
        _configManager = configManager;
        _log = log;
        _eventRegistry = eventRegistry;
        eisHostIp = UtilityClass.GetLocalIpAddress();
        _competingConsumerDbContext.SetHostIpAddress(eisHostIp);
        _OutboundTopic = configManager.GetAppSettings().OutboundTopic;
        _InboundQueue = _configManager.GetAppSettings().InboundQueue;

        if (_configManager.GetMessageSubscriptionStatus())
        {
            ConsumerKeepAliveTask().ConfigureAwait(false);
        }
    }

    public async Task InboxOutboxPollerTask()
    {
        if (!GlobalVariables.IsTransportInterrupted)
        {
            await ProcessAllUnProcessedOutboxEvents();
        }

        if (GlobalVariables.IsCurrentIpLockedForConsumer)
        {
            await ProcessAllUnprocessedInboxEvents();
        }
        else
        {
            _log.LogInformation("QuartzInboxoutboxPollerJob >> not locked for broker connection");
        }
    }


    private async Task ProcessAllUnprocessedInboxEvents()
    {
        var inboxEventsList = await _eventInboxOutboxDbContext.GetAllUnprocessedEvents(AtLeastOnceDeliveryDirection.IN, _InboundQueue);

        if (inboxEventsList != null && inboxEventsList.Count > 0)
        {
            string _eventID = null;
            _log.LogInformation("INBOX: UnprocessedInboxEvents data are available: {c}", inboxEventsList.Count);
            foreach (var events in inboxEventsList)
            {
                try
                {
                    _eventID = events.EventId;
                    _log.LogDebug(events.EisEvent);
                    EisEvent eisEvent = JsonSerializerUtil.DeserializeObject<EisEvent>(events.EisEvent);
                    _log.LogDebug("Processing Event ID: {_eventID}", events.EventId);
                    if (_eventID == null)
                    {
                        _log.LogCritical("Event ID us null for:", events.EventId);
                        throw new Exception("ProcessAllUnprocessedInboxEvents: Event ID returned null");
                    }

                    await ConsumeEvent(eisEvent, events.TopicQueueName);
                    var recordUpdatesStatus = await _eventInboxOutboxDbContext.UpdateEventStatus(_eventID, _InboundQueue, EisSystemVariables.PROCESSED, AtLeastOnceDeliveryDirection.IN);
                    _log.LogInformation("Processed {e}, with status {s} ", _eventID.ToString(), recordUpdatesStatus);
                }
                catch (Exception ex)
                {
                    _log.LogError("Exception occurred while processing > {e}", ex.StackTrace);

                    if (_eventID != null)
                    {
                        await _eventInboxOutboxDbContext.UpdateEventStatus(_eventID, _InboundQueue, EisSystemVariables.FAILED, AtLeastOnceDeliveryDirection.IN);
                    }
                }
            }
        }
        else
        {
            GlobalVariables.IsUnprocessedInMessagePresentInDB = false;
        }
    }

    private async Task ProcessAllUnProcessedOutboxEvents()
    {
        var outboxEventsList = await _eventInboxOutboxDbContext.GetAllUnprocessedEvents(AtLeastOnceDeliveryDirection.OUT, _OutboundTopic);
        if (outboxEventsList != null && outboxEventsList.Count > 0)
        {
            string _eventID = null;

            _log.LogInformation("OUTBOX: UnprocessedOutboxEvents data are available: {c}", outboxEventsList.Count);
            EisEvent eisEvent = null;

            foreach (var events in outboxEventsList)
            {
                try
                {
                    _eventID = events.EventId;
                    eisEvent = JsonSerializerUtil.DeserializeObject<EisEvent>(events.EisEvent);

                    if (eisEvent is null || _eventID is null)
                    {
                        throw new Exception("ProcessAllUnprocessedOutBoxEvents::Event ID returned null");
                    }

                    await PublishToTopic(eisEvent);
                }
                catch (Exception ex)
                {
                    _log.LogError("Exception occurred while processing > {e}", ex.StackTrace);

                    if (eisEvent == null || _eventID == null)
                    {
                        await _eventInboxOutboxDbContext.UpdateEventStatus(_eventID, _OutboundTopic, EisSystemVariables.FAILED, AtLeastOnceDeliveryDirection.OUT);
                    }
                }
            }
        }
        else
        {
            GlobalVariables.IsUnprocessedOutMessagePresentInDB = false;
        }
    }

    public async Task TryPublish(EisEvent eisEvent)
    {
        List<EisEventInboxOutbox> outboxEventList = await _eventInboxOutboxDbContext.GetAllUnprocessedEvents(AtLeastOnceDeliveryDirection.OUT, _OutboundTopic);
        var recordInsertCount = await _eventInboxOutboxDbContext.TryEventInsert(eisEvent, _OutboundTopic, AtLeastOnceDeliveryDirection.OUT);

        if (recordInsertCount == 1)
        {
            _log.LogInformation("OUTBOX::New [Insert] status: {a}", recordInsertCount);

            if (outboxEventList != null && outboxEventList.Count > 0)
            {
                await ProcessAllUnProcessedOutboxEvents();
            }
            else
            {
                try
                {
                    await PublishToTopic(eisEvent);
                }
                catch (Exception ex)
                {
                    _log.LogError("Exception occurred in TryPublish while processing > {e}", ex.StackTrace);
                    throw;
                }
            }
        }
        else
        {
            _log.LogInformation("OUTBOX::OLD record already published. Insert status: {a}", recordInsertCount);
        }
    }

    private async Task PublishToTopic(EisEvent eisEvent)
    {
        try
        {
            _brokerConnectionFactory.PublishTopic(eisEvent);
        }
        catch (Exception ex)
        {
            _log.LogError("Exception {e} while publishing event id: {event} ", ex, eisEvent.EventId);
            throw;
        }

        var recordUpdatesStatus = await _eventInboxOutboxDbContext.UpdateEventStatus(eisEvent.EventId, _OutboundTopic, EisSystemVariables.PROCESSED, AtLeastOnceDeliveryDirection.OUT);
        _log.LogInformation("OUTBOX::Processed {e}, with status {s}", eisEvent.EventId.ToString(), recordUpdatesStatus);
    }

    public async Task ConsumeEvent(EisEvent eisEvent, string queueName)
    {
        await UtilityClass.ConsumeEventAsync(eisEvent, queueName, _eventRegistry, _log);
    }

    public async Task ConsumerKeepAliveTask()
    {
        var eisGroupKey = _configManager.GetSourceSystemName() + "_COMPETING_CONSUMER_GROUP";
        var refreshInterval = _configManager.GetBrokerConfiguration().RefreshInterval;

        try
        {
            var hostIP = eisHostIp;
            var deleteResult = await _competingConsumerDbContext.DeleteStaleEntry(eisGroupKey, refreshInterval);
            _log.LogInformation("Stale entry delete status : {r}", deleteResult);
            int insertResult = await _competingConsumerDbContext.InsertEntry(eisGroupKey);

            if (insertResult == 1)
            {
                _brokerConnectionFactory.CreateConsumerListener();
                _log.LogInformation("*** Consumer locked for: {ip} in group: {groupKey}", hostIP, eisGroupKey);
                GlobalVariables.IsCurrentIpLockedForConsumer = true;
            }
            else
            {
                string ipAddress = _competingConsumerDbContext.GetIpAddressOfServer(eisGroupKey, refreshInterval);
                _log.LogInformation("IP Address from server::" + ipAddress);
                
                if (ipAddress != null)
                {
                    _log.LogInformation($"Current IP: [{eisHostIp}]");
                    _log.LogInformation("IsIPAddressMatchesWithGroupEntry(IpAddress): " + IsIPAddressMatchesWithGroupEntry(ipAddress));

                    if (!IsIPAddressMatchesWithGroupEntry(ipAddress))
                    {
                        _brokerConnectionFactory.DestroyConsumerConnection();
                        GlobalVariables.IsCurrentIpLockedForConsumer = false;
                    }
                    else
                    {
                        _brokerConnectionFactory.CreateConsumerListener();
                        
                        if (!GlobalVariables.IsTransportInterrupted)
                        {
                            var keepAliveResult = _competingConsumerDbContext.KeepAliveEntry(true, eisGroupKey);
                            _log.LogInformation("*** Refreshing Keep Alive entry {k}", keepAliveResult.Result);
                            GlobalVariables.IsCurrentIpLockedForConsumer = true;
                        }
                        else
                        {
                            _log.LogInformation("Keep alive entry will start once broker connections are fully established");
                        }
                    }
                }
                else
                {
                    _brokerConnectionFactory.DestroyConsumerConnection();
                    _log.LogInformation("***Connection destroyed");
                }
            }
            _log.LogInformation("Existing QuartzKeepAliveEntryJob");
        }
        catch (Exception ex)
        {
            _log.LogCritical("Exception when creating consumer: {e}", ex.Message);
            _brokerConnectionFactory.DestroyConsumerConnection();
            _log.LogCritical("Consumer connection stopped on IP:" );
        }
    }

    private bool IsIPAddressMatchesWithGroupEntry(string ipAddress)
    {
        bool flag = ipAddress.Equals(eisHostIp);

        if (flag)
        {
            GlobalVariables.IsCurrentIpLockedForConsumer = true;
        }
        return flag;
    }

    public Task ConsumerEvent(EisEvent eisEvent, string queueName)
    {
        throw new NotImplementedException();
    }
}