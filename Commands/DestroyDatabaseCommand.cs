using System.Threading.Tasks;
using CommandLine;
using TestEnvironmentTool.Infrastructure;

namespace TestEnvironmentTool.Commands
{
    [Verb("destroy", HelpText = "Destroy the database container.")]
    public class DestroyDatabaseCommand : BaseCommand
    {
        public Task<int> Execute(DatabaseContainerFactory databaseContainerFactory)
        {
            return Task.FromResult(TryExecute(databaseContainerFactory.DestroyContainer));
        }
    }
}
