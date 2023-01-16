using System.Runtime.InteropServices.ComTypes;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Net;
using System.Net.NetworkInformation;
using EIS.Domain.Entities;
using EIS.Application.Interfaces;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using EIS.Application.Exceptions;
using System;

namespace EIS.Application.Util
{
    public class UtilityClass
    {
        public static string? GetLocalIpAddress()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }

            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in hostEntry.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }

            return null;
        }

        public static async Task ConsumeEventAsync(EisEvent eisEvent, string queueName, IEventHandlerRegistry eventRegistry, ILogger log)
        {
            IMessageProcessor messageProcessor = eventRegistry.GetMessageProcessor();

            if (messageProcessor == null)
            {
                log.LogError("No message handler found for the event ID {id} in queue {queue}", eisEvent.EventId, queueName);
                throw new EisMessageProcessException("No Message Processor found for the queue");
            }

            try 
            {
                log.LogError("Message with event {event} received", eisEvent);
            }
            catch (Exception e)
            {
                log.LogError("Processing of message with id {id} failed with error {er}", eisEvent.EventId, e.Message);
                throw new EisMessageProcessException($"Processing event ID => {eisEvent.EventId}", e);
            }
        }
    }
}
