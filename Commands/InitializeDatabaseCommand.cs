using System.Threading.Tasks;
using CommandLine;
using TestEnvironmentTool.Infrastructure;

namespace TestEnvironmentTool.Commands
{
    [Verb("init", HelpText = "Initialize database container.")]
    public class InitializeDatabaseCommand : BaseCommand
    {
        public async Task<int> Execute(DatabaseContainerFactory databaseContainerFactory)
        {
            var result = await TryExecuteAsync(
                async () =>
                {
                    _ = databaseContainerFactory.GetContainer();

                    await databaseContainerFactory.DatabaseHealthCheck();
                });

            return result;
        }
    }
}
