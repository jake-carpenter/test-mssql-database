using System;
using System.IO;
using System.Threading.Tasks;
using Spectre.Console;

namespace TestEnvironmentTool.Commands
{
    public abstract class BaseCommand
    {
        protected async Task<int> TryExecuteAsync(Func<Task> action)
        {
            try
            {
                await action();
                return 0;
            }
            catch (Exception e)
            {
                OutputError(e);
                return 1;
            }
        }

        protected int TryExecute(Action action)
        {
            try
            {
                action();
                return 0;
            }
            catch (Exception e)
            {
                OutputError(e);
                return 1;
            }
        }

        protected DirectoryInfo GetWorkingDirectory(string workingPath)
        {
            var directory = new DirectoryInfo(workingPath);

            if (!directory.Exists)
                throw new Exception("Invalid working directory. Provide a valid working directory with '-w <path>'");

            return directory;
        }

        private void OutputError(Exception exception)
        {
            Console.WriteLine();
            AnsiConsole.MarkupLine($"[red]{exception.Message}[/]");
            Console.WriteLine(exception.StackTrace);
        }
    }
}
