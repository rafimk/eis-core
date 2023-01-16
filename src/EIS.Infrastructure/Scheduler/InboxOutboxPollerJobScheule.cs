using EIS.Application.Interfaces;
using EIS.Infrastructure.Scheduler.Jobs;
using System;

namespace EIS.Infrastructure.Scheduler;

public class InboxOutboxPollerJobSchedule : IJobSchedule
{
    private readonly IConfigurationManager _configManager;

    public InboxOutboxPollerJobSchedule(IConfigurationManager configManager)
    {
        _configManager = configManager;
        JobType = typeof(InboxOutboxPollerJob);

    }

    public Type JobType { get; }
    
    public string GetCronExpression()
    {
        return _configManager.GetBrokerConfiguration().InboxOutboxTimerPeriod;
    }
}