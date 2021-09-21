using System.Threading.Tasks;
using CommandLine;
using TestEnvironmentTool.Infrastructure;

namespace TestEnvironmentTool.Commands
{
    [Verb("init", HelpText = "Initialize database container.")]
    public class InitializeDatabaseCommand : BaseCommand
    {
        public Task<int> Execute(DatabaseContainerFactory databaseContainerFactory)
        {
            var result = TryExecute(
                () =>
                {
                    var container = databaseContainerFactory.GetContainer();

                    databaseContainerFactory.WaitForHealthyDatabase(container);
                });

            return Task.FromResult(result);
        }
    }
}
