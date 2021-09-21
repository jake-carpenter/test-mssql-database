using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Model.Builders;
using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Services.Extensions;
using Spectre.Console;

namespace TestEnvironmentTool.Infrastructure
{
    public class SchemaPullManager
    {
        private readonly SchemaPullSettings _settings;
        private readonly string _relativeSqlPath;
        private const string CompletedMessage = "COMPLETED SCHEMA PULLS";

        public SchemaPullManager(Settings settings)
        {
            _settings = settings.SchemaPullSettings;
            _relativeSqlPath = settings.LoadSchemaSettings.SqlSourceFolderRelativeToWorkingDirectory;
        }

        public async Task PullSchema(DirectoryInfo workingPath)
        {
            var buildTask = Task.Run(BuildPythonImage);
            var sqlDirectory = GetSqlDirectory(workingPath);
            var scripterCommands = BuildScripterCommands(sqlDirectory);
            BackupExistingSchemaPulls(sqlDirectory);

            await buildTask;
            using var container = BuildPythonContainer(scripterCommands, sqlDirectory);

            AnsiConsole.MarkupLine("[grey]Starting python container...[/]");
            container.Start();
            AnsiConsole.MarkupLine("[green]Starting python container...done[/]");

            WaitForPullToComplete(container);
        }

        public void BuildPythonImage()
        {
            AnsiConsole.MarkupLine("[grey]Building python image...[/]");

            new Builder()
                .DefineImage(_settings.CustomPythonImageName)
                .ReuseIfAlreadyExists()
                .From(_settings.PythonImageTag)
                .Run("echo 'deb http://ftp.us.debian.org/debian/ jessie main' >>/etc/apt/sources.list")
                .Run("apt-get update --allow-insecure-repositories")
                .Run("apt-get install --allow-unauthenticated -y libicu63 libssl1.0.0 libffi-dev libunwind8")
                .Run("pip install --upgrade pip")
                .Run("pip install mssql-scripter")
                .ToImage()
                .Build()
                .Start();

            AnsiConsole.MarkupLine("[green]Building python image...done[/]");
        }

        public IContainerService BuildPythonContainer(IEnumerable<string> scripterCommands, DirectoryInfo sqlPath)
        {
            AnsiConsole.MarkupLine("[grey]Building python container...[/]");
            var containerBuilder = new Builder()
                .UseContainer()
                .UseImage(_settings.CustomPythonImageName)
                .WithName(_settings.ContainerName)
                .Mount(sqlPath.FullName, "/sql", MountType.ReadWrite);

            var commands = string.Join("; ", scripterCommands);
            commands += $"; echo {CompletedMessage}";

            var container = containerBuilder.Command($"/bin/bash -c \"{commands}\"").Build();
            container.RemoveOnDispose = true;
            AnsiConsole.MarkupLine("[green]Building python container...done[/]");

            return container;
        }

        public void WaitForPullToComplete(IContainerService container)
        {
            var seconds = _settings.TimeoutInSeconds;
            AnsiConsole.MarkupLine($"[grey]Waiting for schema pull to complete (up to {seconds}sec)...[/]");
            container.WaitForMessageInLogs(CompletedMessage, _settings.TimeoutInSeconds * 1000);
            AnsiConsole.MarkupLine($"[green]Waiting for schema pull to complete (up to {seconds}sec)...done[/]");
        }

        public IEnumerable<string> BuildScripterCommands(DirectoryInfo sqlDirectory)
        {
            const string templatedOptions = "-S {0} -d {1} -U {2} -P {3} -f {4} --include-objects {5}";
            var flags = string.Join(' ', _settings.MssqlScripterFlags);

            foreach (var source in _settings.Sources)
            {
                var objects = string.Join(' ', source.Objects);
                var options = string.Format(
                    templatedOptions,
                    source.Host,
                    source.Database,
                    _settings.Username,
                    _settings.Password,
                    $"/sql/{source.Database}-SchemaDump.sql",
                    objects);

                AnsiConsole.MarkupLine(
                    $"[yellow]Will request objects from {source.Database} on {source.Host}: {objects}[/]");
                yield return $"mssql-scripter {flags} {options}";
            }
        }

        public void BackupExistingSchemaPulls(DirectoryInfo sqlPath)
        {
            const string dumpSuffix = "-SchemaDump";

            var backupDirectory = new DirectoryInfo(Path.Combine(sqlPath.FullName, "backups"));
            if (!backupDirectory.Exists)
            {
                backupDirectory.Create();
            }

            var filesToBackup = sqlPath.GetFiles($"*{dumpSuffix}.sql");
            if (filesToBackup.Length == 0)
                return;

            var dateSuffix = $"{DateTime.Now:-yyyyMMdd-hhmmss}";
            AnsiConsole.MarkupLine("[grey]Backing up existing schema dump files...[/]");

            foreach (var file in filesToBackup)
            {
                var databasePrefix = file.Name.Split(dumpSuffix)[0];
                var target = Path.Combine(
                    backupDirectory.FullName,
                    Path.GetFileName($"{databasePrefix}{dumpSuffix}{dateSuffix}.sql"));
                File.Move(file.FullName, target);
            }

            AnsiConsole.MarkupLine("[green]Backing up existing schema dump files...done[/]");
        }

        private DirectoryInfo GetSqlDirectory(DirectoryInfo workingPath)
        {
            var fullSqlPath = Path.Combine(workingPath.FullName, _relativeSqlPath);
            var sqlDirectory = new DirectoryInfo(fullSqlPath);

            if (!sqlDirectory.Exists)
                throw new Exception($"Invalid path for SQL dump to write: {sqlDirectory.FullName}");

            return sqlDirectory;
        }
    }
}
