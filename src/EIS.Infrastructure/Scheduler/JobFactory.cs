namespace EIS.Infrastructure.Scheduler

public class JobFactory : IJobFactory
{
    private readonly IServiceProvider _serviceProvider;

    public JobFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ReturnJob(IJob job)
    {
        
    }
}