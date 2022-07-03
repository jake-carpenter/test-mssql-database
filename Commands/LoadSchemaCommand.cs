using System.Threading.Tasks;
using CommandLine;
using Spectre.Console;
using TestEnvironmentTool.Infrastructure;

namespace TestEnvironmentTool.Commands
{
    [Verb("load-schema", HelpText = "Load schema from SQL files and migrations into the database container.")]
    public class LoadSchemaCommand : BaseCommand
    {
        [Option(
            'w',
            "working-path",
            HelpText = "Relative or full path to path containing SQL and migration projects.",
            Default = "./",
            Required = false)]
        public string WorkingPath { get; set; }

        public async Task<int> Execute(DatabaseSchemaWriter databaseSchemaWriter)
        {
            var workingDirectory = GetWorkingDirectory(WorkingPath);
            AnsiConsole.MarkupLine($"[yellow]Using working path[/]: [grey]{workingDirectory.FullName}[/]");

            return await TryExecuteAsync(
                async () => await databaseSchemaWriter.LoadFullSchema(workingDirectory));
        }
    }
}
