using System.Threading.Tasks;
using CommandLine;
using Spectre.Console;
using TestEnvironmentTool.Infrastructure;

namespace TestEnvironmentTool.Commands
{
    [Verb("pull-schema", HelpText = "Pull schema for configured database objects.")]
    public class PullSchemaCommand : BaseCommand
    {
        [Option(
            'w',
            "working-path",
            HelpText = "Relative or full path to path containing SQL and migration projects.",
            Default = "./",
            Required = false)]
        public string WorkingPath { get; set; }

        public async Task<int> Execute(SchemaPullManager schemaPullManager)
        {
            var workingDirectory = GetWorkingDirectory(WorkingPath);
            AnsiConsole.MarkupLine($"[yellow]Using working path[/]: [grey]{workingDirectory.FullName}[/]");

            return await TryExecuteAsync(
                async () => await schemaPullManager.PullSchema(workingDirectory));
        }
    }
}
