namespace Aurora.Flowboard.Infrastructure.Outbox;

internal sealed class ConfigureProcessOutboxJob(IOptions<OutboxOptions> options) : IConfigureOptions<QuartzOptions>
{
    private readonly OutboxOptions _outboxOptions = options.Value;

    public void Configure(QuartzOptions options)
    {
        var jobName = typeof(ProcessOutboxJob).FullName!;

        options
            .AddJob<ProcessOutboxJob>(cfg => cfg.WithIdentity(jobName))
            .AddTrigger(cfg => cfg
                .ForJob(jobName)
                .WithSimpleSchedule(a => a
                    .WithIntervalInSeconds(_outboxOptions.IntervalInSeconds)
                    .RepeatForever()));
    }
}
