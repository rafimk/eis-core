using Quartz;
using Quartz.Spi;
using System;

namespace EIS.Infrastructure.Scheduler;

public class JobFactory : IJobFactory
{
    private readonly IServiceProvider _serviceProvider;

    public JobFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
    {
    }

    public void ReturnJob(IJob job)
    {
    }
}