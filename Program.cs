using CommandLine;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using System.IO;
using System.Threading.Tasks;
using TestEnvironmentTool.Commands;
using TestEnvironmentTool.Infrastructure;

namespace TestEnvironmentTool
{
    public static class Program
    {
        private static Settings Settings { get; set; }

        public static async Task<int> Main(string[] args)
        {
            Settings = GetBoundSettings();

            var schemaPullManager = new SchemaPullManager(Settings);
            var databaseContainerFactory = new DatabaseContainerFactory(Settings);
            var databaseSchemaWriter = new DatabaseSchemaWriter(Settings);

            var result = await Parser.Default
                .ParseArguments<
                    PullSchemaCommand,
                    InitializeDatabaseCommand,
                    LoadSchemaCommand,
                    DestroyDatabaseCommand,
                    ResetCommand
                >(args).MapResult(
                    (PullSchemaCommand cmd) => cmd.Execute(schemaPullManager),
                    (InitializeDatabaseCommand cmd) => cmd.Execute(databaseContainerFactory),
                    (LoadSchemaCommand cmd) => cmd.Execute(databaseSchemaWriter),
                    (DestroyDatabaseCommand cmd) => cmd.Execute(databaseContainerFactory),
                    (ResetCommand cmd) => cmd.Execute(
                        databaseContainerFactory,
                        databaseSchemaWriter,
                        schemaPullManager),
                    errors =>
                    {
                        foreach (var error in errors)
                        {
                            AnsiConsole.MarkupLine($"[red]\n{error}\n[/]");
                        }

                        return Task.FromResult(1);
                    });

            return result;
        }

        private static Settings GetBoundSettings()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();
            var settings = new Settings();
            configuration.GetSection(nameof(Settings)).Bind(settings);

            return settings;
        }

        private static DirectoryInfo GetFullPath(string path)
        {
            return new DirectoryInfo(path);
        }
    }
}
