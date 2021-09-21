using Ductus.FluentDocker;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Services.Extensions;
using Spectre.Console;

namespace TestEnvironmentTool.Infrastructure
{
    public class DatabaseContainerFactory
    {
        private readonly Settings _settings;

        public DatabaseContainerFactory(Settings settings)
        {
            _settings = settings;
        }

        public IContainerService GetContainer()
        {
            AnsiConsole.MarkupLine("[grey]Building database container...[/]");

            var container = new Builder()
                .UseContainer()
                .UseImage(_settings.InitializeDatabaseSettings.MsSqlDockerImage)
                .KeepContainer()
                .KeepRunning()
                .WithEnvironment(
                    "ACCEPT_EULA=Y",
                    $"MSSQL_SA_PASSWORD={_settings.InitializeDatabaseSettings.DatabasePassword}")
                .WithName(_settings.InitializeDatabaseSettings.ContainerName)
                .ReuseIfExists()
                .ExposePort(_settings.InitializeDatabaseSettings.Port, 1433)
                .Build();

            AnsiConsole.MarkupLine("[green]Building database container...done[/]");

            if (container.State != ServiceRunningState.Running)
            {
                AnsiConsole.MarkupLine("[grey]Starting database container...[/] ");
                container.Start();
            }

            AnsiConsole.MarkupLine("[green]Starting database container...done[/]");

            return container;
        }

        public void WaitForHealthyDatabase(IContainerService container)
        {
            var timeoutInSeconds = _settings.InitializeDatabaseSettings.HealthCheckTimeoutInSeconds;
            AnsiConsole.MarkupLine($"[grey]Waiting for database to be up (up to {timeoutInSeconds}sec)...[/]");
            container.WaitForMessageInLogs("The tempdb database has", timeoutInSeconds * 1000);
            AnsiConsole.MarkupLine($"[green]Waiting for database to be up (up to {timeoutInSeconds}sec)...done[/]");
        }

        public void DestroyContainer()
        {
            AnsiConsole.MarkupLine("[grey]Looking for existing database containers...[/]");

            var dockerServices = Fd.Discover();
            foreach (var dockerService in dockerServices)
            {
                var containers = dockerService.GetContainers(all: true);
                AnsiConsole.MarkupLine("[green]Looking for existing database containers...done[/]");

                foreach (var container in containers)
                {
                    if (container.Name != _settings.InitializeDatabaseSettings.ContainerName)
                        continue;

                    AnsiConsole.MarkupLine($"[grey]Removing database container '{container.Id[..8]}'...[/]");

                    if (container.State == ServiceRunningState.Running)
                    {
                        container.Stop();
                    }

                    container.Remove(force: true);
                    AnsiConsole.MarkupLine($"[green]Removing database container '{container.Id[..8]}'...done[/]");
                }
            }
        }
    }
}
