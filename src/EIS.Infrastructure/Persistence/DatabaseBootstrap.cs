using System.IO;
using System.Reflection;
using EIS.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using EIS.Application.Constants;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Collections.Generic;
using EIS.Domain.Entities;

namespace EIS.Infrastructure.Persistence;

public class DatabaseBootstrap : IDatabaseBootstrap
{
    private readonly string _databaseName;
    private readonly IConfiguration _configuration;
    private readonly IEventInboxOutboxDbContext _eventINOUTDbContext;
    private readonly string _OutboundTopic;
    private readonly string _InboundQueue;

    public DatabaseBootstrap(IConfiguration configuration, IConfigurationManager configManager, IEventInboxOutboxDbContext eventINOUTDbContext)
    {
        _configuration = configuration;
        _eventINOUTDbContext = eventINOUTDbContext;
        _databaseName = configuration.GetConnectionString("DefaultConnection");
        Setup();
        _OutboundTopic = configManager.GetAppSettings().OutboundTopic;
        _InboundQueue = configManager.GetAppSettings().InboundQueue;

        InitiateUnprocessedINOUTMessages();
    }

    public void Setup()
    {
        Console.WriteLine("Setup....");
        using (var connection = new SqlConnection(_databaseName))
        {
            try
            {
                Console.WriteLine("Before executing schema. sql");

                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "EISCore.schema.sql";

                string result = "";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))

                using (StreamReader reader = new StreamReader(stream))
                {
                    result = reader.ReadToEnd();
                }

                connection.ExecuteAsync(result);

                Console.WriteLine($"Created database {_databaseName}");
                GlobalVariables.IsDatabaseTablesInitialized = true;
            }
            catch (Exception ex)
            {
                if (ex.Message.StartsWith("There is already an object named"))
                {
                    GlobalVariables.IsDatabaseTablesInitialized = true;
                }
                else if (ex.Message.StartsWith("Database Error:Invalid object name"))
                {
                    GlobalVariables.IsDatabaseTablesInitialized = true;
                }
                else
                {
                    GlobalVariables.IsDatabaseTablesInitialized = false;
                }
                Console.WriteLine("Exception creating database: " + ex.Message);
            }
        }
    }

    public async void InitiateUnprocessedINOUTMessages()
    {
        List<EisEventInboxOutbox> inboxEventList = await _eventINOUTDbContext.GetAllUnprocessedEvents(AtLeastOnceDeliveryDirection.IN, _InboundQueue);
        List<EisEventInboxOutbox> outboxEventList = await _eventINOUTDbContext.GetAllUnprocessedEvents(AtLeastOnceDeliveryDirection.OUT, _OutboundTopic);

        if (inboxEventList != null && inboxEventList.Count > 0)
        {
            GlobalVariables.IsUnprocessedInMessagePresentInDB = true;
        }
        else
        {
            GlobalVariables.IsUnprocessedInMessagePresentInDB = false;
        }

         if (outboxEventList != null && outboxEventList.Count > 0)
        {
            GlobalVariables.isUnprocessedOutMessagePresentInDB = true;
        }
        else
        {
            GlobalVariables.isUnprocessedOutMessagePresentInDB = false;
        }

    }

}