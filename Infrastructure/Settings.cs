using System.Collections.Generic;
using Spectre.Console.Cli;

namespace TestEnvironmentTool.Infrastructure
{
    public class Settings
    {
        public InitializeDatabaseSettings InitializeDatabaseSettings { get; set; }
        public LoadSchemaSettings LoadSchemaSettings { get; set; }
        public SchemaPullSettings SchemaPullSettings { get; set; }
    }

    public class InitializeDatabaseSettings
    {
        public string MsSqlDockerImage { get; set; }
        public string ContainerName { get; set; }
        public int Port { get; set; }
        public string DatabasePassword { get; set; }
        public int HealthCheckTimeoutInSeconds { get; set; }
    }

    public class LoadSchemaSettings
    {
        public string SqlSourceFolderRelativeToWorkingDirectory { get; set; }
        public string[] DatabasesToCreate { get; set; }
        public Dictionary<string, string[]> SqlFilesToExecute { get; set; }
        public Dictionary<string, string> MigrationProjectsToExecute { get; set; }
    }

    public class SchemaPullSettings : CommandSettings
    {
        public string PythonImageTag { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string CustomPythonImageName { get; set; }
        public string ContainerName { get; set; }
        public int TimeoutInSeconds { get; set; }
        public string[] MssqlScripterFlags { get; set; }
        public SchemaPullSource[] Sources { get; set; }
    }

    public class SchemaPullSource
    {
        public string Host { get; set; }
        public string Database { get; set; }
        public string[] Objects { get; set; }
    }
}
