using System;
using EIS.Domain.Entities;
using EIS.Application.Interfaces;
using Apache.NMS;
using Microsoft.Extensions.Logging;
using Apache.NMS.Util;
using EIS.Application.Util;
using System.Threading.Tasks;
using EIS.Application.Constants;
using System.Text.Json;

namespace EIS.Infrastructure.Configuration;

public class BrokerConnectionFactory : IBrockerConnectionFactory
{
    
    private readonly ILogger<BrokerConnectionFactory> _log;
    private readonly BrokerConfiguration _brokerConfiguration;
    private readonly IConfigurationManager _configManager;
    private readonly IConnectionFactory _factory;

    private static TimeSpan receiveTimeout = TimeSpan.FromSeconds(10);

    private readonly Uri _connectUri;
    private readonly ConnectionInterruptedListener interruptedListener = null;
    private readonly ExceptionListener connectionExceptionListener = null;

    private readonly EventHandlerRegistry _eventRegistry;

    private bool isDisposed = false;
    private ApplicationSettings _appSettings;
    private IMessageConsumer _messageConsumer;
    private IConnection _publisherConnection = null;
    private IConnection _consumerConnection;

    private IEventInboxOutboxDbContext _eventInOutDbContext;

    public BrokerConnectionFactory(IConfigurationManager configurationManager, IEventInboxOutboxDbContext eventInOutDbContext,
    ILogger<BrokerConnectionFactory> log)
    {
        _log = log;
        _configManager = configurationManager;
        var _appSetting = configurationManager.GetAppSettings();
        _brokerConfiguration = _configManager.GetBrokerConfiguration();
        _eventRegistry = new EventHandlerRegistry();
        var brokerUrl = _configManager.GetBrokeUrl();
        _log.LogInformation("Broker Connection Factory >> Initializing broker connections.");
        _log.LogInformation("Broker - {brokerUrl}", brokerUrl);
        _connectUri = new Uri(brokerUrl);
        IConnectionFactory factory = new Apache.NMS.ActiveMQ.ConnectionFactory(_connectUri);

        _factory = factory;
        _eventInOutDbContext = eventInOutDbContext;
        _consumerConnection = null;

        interruptedListener = new ConnectionInterruptedListener(OnConnectionInterruptedListener);
        connectionExceptionListener = new ExceptionListener(OnExceptionListener);
    }

    private void PublishMessageToTopic(string message)
    {

        try
        {
            _publisherConnection = _factory.CreateConnection(_brokerConfiguration.Username, _brokerConfiguration.Password);
            _publisherConnection.RequestTimeout = TimeSpan.FromSeconds(30);

            if (_publisherConnection.IsStarted)
            {
                _log.LogInformation("Producer and consumer connection started");
            }

            ISession publisherSession = _publisherConnection.CreateSession(AcknowledgementMode.ClientAcknowledge);
            ITextMessage textMsg = GetTextMessageRequest(publisherSession, message);

            _log.LogInformation("Created Publisher Connection {con}", _publisherConnection.ToString());

            var topic = _configManager.GetAppSettings().OutboundTopic;
            var topicDestination = SessionUtil.GetTopic(publisherSession, topic);

            _publisherConnection.Start();

            if (_publisherConnection.IsStarted)
            {
                _log.LogInformation("Connection started");
            }

            IMessageProducer messagePublisher = publisherSession.CreateProducer(topicDestination);
            messagePublisher.DeliveryMode = MsgDeliveryMode.Persistent;
            messagePublisher.RequestTimeout = receiveTimeout;
            _log.LogInformation("Created message producer for destination topic : {d}", topicDestination);

            messagePublisher.Send(textMsg);
            _log.LogInformation("Message send to destination topic : {d}", topicDestination);
            DestroyPublisherConnection();
        }
        catch (Apache.NMS.ActiveMQ.ConnectionClosedException e1)
        {
            _log.LogCritical("Connection closed exception thrown while closing a connection. {e1}", e1.StackTrace);
            try
            {
                _log.LogCritical("Stopping Connection...");
                if (_publisherConnection != null)
                {
                    _publisherConnection.Stop();
                }
            }
            catch (Apache.NMS.ActiveMQ.ConnectionClosedException e2)
            {
                _log.LogCritical("Connection closed exception thrown while closing a connection. {e2}", e2.StackTrace);
                if (_publisherConnection != null)
                {
                    _publisherConnection.Stop();
                }
            }
            finally
            {
                if (_publisherConnection != null)
                {
                    _publisherConnection.Stop();
                }
            }
            throw;
        }
        catch (Exception e)
        {
            _log.LogCritical("Error occurred while creation producer : ", e.StackTrace);
            DestroyPublisherConnection();
        }
    }


    private ITextMessage GetTextMessageRequest(ISession publisherSession, string message)
    {
        ITextMessage request = publisherSession.CreateTextMessage(message);
        request.NMSCorrelationID = Guid.NewGuid().ToString();
        return request;
    }

    public void PublishTopic(EisEvent eisEvent)
    {
        try
        {
            string jsonString = JsonSerializerUtil.SerializeEvent(eisEvent);
            _log.LogInformation("{s}", jsonString);
            PublishMessageToTopic(jsonString);
            _log.LogDebug($"SendToQueue exiting");
        }
        catch (Exception ex)
        {
            _log.LogCritical("{s}", ex.StackTrace);
            throw;
        }
    }

    public void CreateConsumerListener()
    {
        try
        {
            _log.LogInformation("CreateConsumer _consumerConnection: >> " + _consumerConnection + " <<");

            if (_consumerConnection != null)
            {
                return;
            }

            _log.LogInformation("Creating new consumer broker connection");
            
            var brokerUri = _configManager.GetBrokeUrl();
            _consumerConnection = _factory.CreateConnection(_brokerConfiguration.Username, _brokerConfiguration.Password);
            
            _log.LogInformation("Connection created, client ID set");
            
            _consumerConnection.ConnectionInterruptedListener += interruptedListener;
            _consumerConnection.ExceptionListener += connectionExceptionListener;
            ISession consumerTcpSession = null;
            // Run the broker connection in a new thread, so the application doesnt hang when the broker is down and application is started at the same time.

            Task.Run((() =>
            {
                consumerTcpSession = _consumerConnection.CreateSession(AcknowledgementMode.ClientAcknowledge);
                _consumerConnection.Start();

                if (_consumerConnection.IsStarted)
                {
                    _log.LogInformation("Consumer connection started");
                }
                else
                {
                    _log.LogInformation("Consumer connection not started, starting");
                }

                if (consumerTcpSession != null)
                {
                    var queue = _appSettings.InboundQueue;
                    var queueDestination = SessionUtil.GetQueue(consumerTcpSession, queue);
                    _log.LogInformation("Created message producer for destination queue: {d}", queueDestination);
                    _messageConsumer = consumerTcpSession.CreateConsumer(queueDestination);
                    GlobalVariables.IsTransportInterrupted = false;
                    _messageConsumer.Listener += new MessageListener(OnMessage);
                }
                else
                {
                    _log.LogInformation("Broker connection not established yet.");
                }
            }));
        }
        catch (Exception ex)
        {
            _log.LogCritical("Error occurred when creating consumer: {e}", ex.StackTrace);
            DestroyConsumerConnection();
            throw;
        }
    }

    protected async Task OnMessage(IMessage receivedMsg)
    {
        EisEvent eisEvent = null;
        var InboundQueue = _configManager.GetAppSettings().InboundQueue;

        try
        {
            _log.LogInformation("Receiving the message inside OnMessage");
            var queueMessage = receivedMsg as ITextMessage;

            _log.LogInformation("Received message with Id: {n} ", queueMessage?.NMSMessageId);
            _log.LogInformation("Received message with text: {n} ", queueMessage?.Text);

            eisEvent = JsonSerializerUtil.DeserializeObject<EisEvent>(queueMessage.Text);

            if (eisEvent == null)
            {
                _log.LogInformation("Could not deserialize the message with text: {n} ", queueMessage?.Text);
                return;
            }

            // Check json deserializer exception handling in IN OUT BOX
            _log.LogInformation("Receiving the message: {eisEvent}", eisEvent.EventId.ToString());

            int recordInsertCount = await _eventInOutDbContext.TryEventInsert(eisEvent, InboundQueue, AtLeastOnceDeliveryDirection.IN);

            // If the record is new, and status is 1 then process data
            if (recordInsertCount == 1)
            {
                _log.LogInformation("INBOX::NEW [Insert] status {a}", recordInsertCount);
                await UtilityClass.ConsumeEventAsync(eisEvent, InboundQueue, _eventRegistry, _log);
                var updateStatus = await _eventINOUTDbContext.UpdateEventStatus(eisEvent.EventId, InboundQueue,  EisSystemVariables.PROCESSED, AtLeastOnceDeliveryDirection.IN);
                _log.LogInformation("INBOX::NEW [Processed] status: {b} ", updateStatus);
            }
            else
            {
                _log.LogInformation("INBOX::OLD record already exists. Insert status: {a}", recordInsertCount);
            }
            receivedMsg.Acknowledge();
        }
        catch (JsonException jsonEx)
        {
            _log.LogError("exception is onMessage, invalid json object : {eisEvent}", jsonEx.Message);
            receivedMsg.Acknowledge();
        }
        catch (Exception ex)
        {
            await _eventINOUTDbContext.UpdateEventStatus(eisEvent?.EventId, InboundQueue, EisSystemVariables.FAILED, AtLeastOnceDeliveryDirection.IN);
            _log.LogError("Exception in OnMessage: {eisEvent}", ex.Message);
            // Should not throw exceptions - Create EISMessageProcessExceptions to catch and update the status.
        }
    }

    protected void OnConnectionInterruptedListener()
    {
        _log.LogCritical("Connection Interrupted.");
        GlobalVariables.IsTransportInterrupted = true;
        DestroyConsumerConnection();
    }

    protected void OnExceptionListener(Exception NMException)
    {
        _log.LogCritical("On Exception Listener: {e}", NMException.GetBaseException());
    }

    #region IDisposable
    public void Dispose()
    {
        if (!isDisposed)
        {
            if (_messageConsumer != null)
            {
                _messageConsumer.Close();
            }

            DestroyConsumerConnection();
            isDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
    #endregion

    #region DestroyConsumerConnection
    public void DestroyConsumerConnection()
    {
        try
        {
            _log.LogInformation("Destroy consumer connection - called, IsTransportInterrupted:" + GlobalVariables.IsTransportInterrupted);
            
            if (GlobalVariables.IsTransportInterrupted)
            {
                _log.LogInformation("Destroy consumer connection transport already stopped");
            }
            else
            {
                if (_consumerConnection != null)
                {
                    _consumerConnection.ConnectionInterruptedListener -= interruptedListener;
                    _consumerConnection.ExceptionListener -= connectionListener;
                    _consumerConnection.Stop();
                    _consumerConnection.Close();
                    _consumerConnection.Dispose();

                    _log.LogInformation("Destroyed consumer connection - disposed");
                }
            }
        }
        catch (Exception ex)
        {
            _log.LogError("Error while disposing the connection", ex.StackTrace);
        }
    }
    #endregion

    #region DestroyPublisherConnection
    public void DestroyPublisherConnection()
    {
        try
        {
            if (GlobalVariables.IsTransportInterrupted)
            {
                _log.LogInformation("Destroy producer connection stopped transport");
            }
            else
            {
                if (_publisherConnection != null)
                {
                    _publisherConnection.Stop();
                    _publisherConnection.Close();
                    _publisherConnection.Dispose();
                    _log.LogInformation("Destroyed producer connection - disposed");
                }
            }
        }
        catch (Exception ex)
        {
            _log.LogError("Error while disposing the connection", ex.StackTrace);
        }
    }
    #endregion
}
