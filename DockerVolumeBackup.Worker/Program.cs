using DockerVolumeBackup.Worker;

var builder = Host.CreateDefaultBuilder(args)
    .UseWindowsService() // This configures the application to run as a Windows Service.
    .ConfigureServices(services =>
    {
        services.AddSingleton<IEnvironment, EnvironmentWrapper>();
        services.AddHttpClient<IDiscordService, DiscordService>();
        services.AddHostedService<DockerVolumeBackupWorker>();
    });

var host = builder.Build();
await host.RunAsync();
