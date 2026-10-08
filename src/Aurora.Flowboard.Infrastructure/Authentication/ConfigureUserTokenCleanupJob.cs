namespace Aurora.Flowboard.Infrastructure.Authentication;

internal sealed class ConfigureUserTokenCleanupJob(IOptions<UserTokenCleanupOptions> options) : IConfigureOptions<QuartzOptions>
{
    private readonly UserTokenCleanupOptions _cleanupOptions = options.Value;

    public void Configure(QuartzOptions options)
    {
        string jobName = typeof(UserTokenCleanupJob).FullName!;

        options
            .AddJob<UserTokenCleanupJob>(cfg => cfg.WithIdentity(jobName))
            .AddTrigger(cfg => cfg
                .ForJob(jobName)
                .WithSimpleSchedule(s => s
                    .WithIntervalInHours(_cleanupOptions.IntervalInHours)
                    .RepeatForever()));
    }
}
