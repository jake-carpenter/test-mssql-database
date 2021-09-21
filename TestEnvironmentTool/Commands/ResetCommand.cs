using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Spectre.Console;
using TestEnvironmentTool.Infrastructure;

namespace TestEnvironmentTool.Commands
{
    [Verb(
        "reset",
        isDefault: true,
        HelpText = "Full database reset. Destroy existing container, reinitialize and reload the schema.")]
    public class ResetCommand : BaseCommand
    {
        [Option(
            'w',
            "working-path",
            HelpText = "Relative or full path to path containing SQL and migration projects.",
            Default = "./",
            Required = false)]
        public string WorkingPath { get; set; }

        [Option(
            'p',
            "pull-schema",
            HelpText = "Include pull schema before loading the schema.",
            Default = false,
            Required = false)]
        public bool PullSchema { get; set; }

        public async Task<int> Execute(
            DatabaseContainerFactory databaseContainerFactory,
            DatabaseSchemaWriter databaseSchemaWriter,
            SchemaPullManager schemaPullManager)
        {
            var workingDirectory = GetWorkingDirectory(WorkingPath);
            AnsiConsole.MarkupLine($"[yellow]Using working path[/]: [grey]{workingDirectory.FullName}[/]");
            return await TryExecuteAsync(async () => await Reset());

            async Task Reset()
            {
                var startupTasks = new List<Task>();

                if (PullSchema)
                {
                    startupTasks.Add(Task.Run(() => schemaPullManager.PullSchema(workingDirectory)));
                }

                var databaseContainerTask = Task.Run(
                    () =>
                    {
                        databaseContainerFactory.DestroyContainer();
                        var container = databaseContainerFactory.GetContainer();
                        databaseContainerFactory.WaitForHealthyDatabase(container);

                        return container;
                    });

                startupTasks.Add(databaseContainerTask);

                var migrationProjects = databaseSchemaWriter.GetFullMigrationPaths(workingDirectory).ToArray();
                startupTasks.Add(databaseSchemaWriter.BuildMigrations(migrationProjects));

                await Task.WhenAll(startupTasks);

                var sqlStatements = await databaseSchemaWriter.ReadSql(workingDirectory);
                await databaseSchemaWriter.CreateDatabases();

                var executeSqlTask = databaseSchemaWriter.LoadSchemasFromSql(sqlStatements);
                var runMigrationsTask = databaseSchemaWriter.RunMigrations(migrationProjects);

                await Task.WhenAll(executeSqlTask, runMigrationsTask);
            }
        }
    }
}
