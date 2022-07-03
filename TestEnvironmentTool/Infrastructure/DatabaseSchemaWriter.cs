using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CliWrap;
using Dapper;
using Microsoft.Data.SqlClient;
using Spectre.Console;

namespace TestEnvironmentTool.Infrastructure
{
    public class DatabaseSchemaWriter
    {
        private readonly string _connectionStringBase;
        private readonly LoadSchemaSettings _settings;

        public DatabaseSchemaWriter(Settings settings)
        {
            _settings = settings.LoadSchemaSettings;

            var port = settings.InitializeDatabaseSettings.Port;
            var password = settings.InitializeDatabaseSettings.DatabasePassword;
            _connectionStringBase =
                $"Data Source=localhost,{port};User Id=sa;Password={password};TrustServerCertificate=true";
        }

        public async Task LoadFullSchema(DirectoryInfo workingPath)
        {
            try
            {
                var createDatabaseTask = CreateDatabases();
                var readSqlTask = ReadSql(workingPath);
                var migrationProjects = GetFullMigrationPaths(workingPath).ToArray();
                var buildTask = BuildMigrations(migrationProjects, workingPath);

                await Task.WhenAll(createDatabaseTask, readSqlTask, buildTask);

                var loadSqlTask = LoadSchemasFromSql(readSqlTask.Result);
                await buildTask;
                var runMigrationsTask = RunMigrations(migrationProjects, workingPath);

                await Task.WhenAll(loadSqlTask, runMigrationsTask);
                CleanupTemporaryFolders(workingPath);
            }
            catch
            {
                CleanupTemporaryFolders(workingPath);
                throw;
            }
        }

        public async Task CreateDatabases()
        {
            const string createSql = "CREATE DATABASE";

            AnsiConsole.MarkupLine("[grey]Creating databases...[/]");

            var stringBuilder = new StringBuilder();
            foreach (var dbName in _settings.DatabasesToCreate)
            {
                stringBuilder.Append($"{createSql} {dbName};\n");
            }

            using var connection = new SqlConnection(_connectionStringBase);
            await connection.ExecuteAsync(stringBuilder.ToString());

            AnsiConsole.MarkupLine("[green]Creating databases...done[/]");
        }

        public async Task<Dictionary<string, List<string>>> ReadSql(DirectoryInfo workingDirectory)
        {
            var sqlPath = Path.Combine(workingDirectory.FullName, _settings.SqlSourceFolderRelativeToWorkingDirectory);
            var sqlDirectory = new DirectoryInfo(sqlPath);
            if (!sqlDirectory.Exists)
                throw new Exception($"Invalid directory for SQL files: {sqlDirectory.FullName}");

            var databaseStatements = new Dictionary<string, List<string>>();
            if (_settings.SqlFilesToExecute is null)
                return databaseStatements;

            var statementsToSkip = new[] { "SET ANSI_PADDING ON", "SET ANSI_NULLS ON", "SET QUOTED_IDENTIFIER ON" };
            foreach (var (database, sqlFiles) in _settings.SqlFilesToExecute)
            {
                if (!databaseStatements.TryGetValue(database, out var statements))
                {
                    statements = new List<string>();
                    databaseStatements.Add(database, statements);
                }

                foreach (var fileName in sqlFiles)
                {
                    var text = await File.ReadAllTextAsync($"{sqlDirectory.FullName}/{fileName}");
                    var sqlCommands = text
                        .Split("GO")
                        .Select(sql => sql.Trim())
                        .Where(sql => sql.Length > 0 && !statementsToSkip.Contains(sql))
                        .ToArray();

                    statements.AddRange(sqlCommands);
                }
            }

            return databaseStatements;
        }

        public async Task LoadSchemasFromSql(Dictionary<string, List<string>> databaseStatements)
        {
            var tasks = databaseStatements.Select(
                async dbStatements =>
                {
                    var (database, statements) = dbStatements;
                    var connectionString = $"{_connectionStringBase};Database={database}";
                    AnsiConsole.MarkupLine($"[grey]Executing {statements.Count} statements on '{database}'...[/]");
                    using var connection = new SqlConnection(connectionString);

                    foreach (var statement in statements)
                    {
                        await connection.ExecuteAsync(statement);
                    }

                    AnsiConsole.MarkupLine($"[green]Executing {statements.Count} statements on '{database}'...done[/]");
                });

            await Task.WhenAll(tasks);
        }

        public Task BuildMigrations(IEnumerable<MigrationProject> migrationProjects, DirectoryInfo workingDirectory)
        {
            async void ExecuteBuildCommand(MigrationProject project)
            {
                AnsiConsole.MarkupLine($"[grey]Building {project.Database} migrations...[/]");

                var tempDir = Path.Combine(workingDirectory.FullName, $"./tmp_{project.Database}");
                await Cli.Wrap("dotnet")
                    .WithArguments($"build {project.FullPath} -c Release -o {tempDir}")
                    .WithStandardErrorPipe(PipeTarget.ToDelegate(err => AnsiConsole.MarkupLine($"[red]{err}[/]")))
                    .ExecuteAsync();

                AnsiConsole.MarkupLine($"[green]Building {project.Database} migrations...done[/]");
            }

            Parallel.ForEach(migrationProjects, ExecuteBuildCommand);

            return Task.CompletedTask;
        }

        public async Task RunMigrations(IEnumerable<MigrationProject> migrationProjects, DirectoryInfo workingDirectory)
        {
            async Task ExecuteRunCommand(MigrationProject project)
            {
                var connectionString = $"{_connectionStringBase};Database={project.Database}";
                var tempDir = Path.Combine(workingDirectory.FullName, $"./tmp_{project.Database}");
                AnsiConsole.MarkupLine($"[grey]Executing {project.Database} migrations[/]");

                await Cli.Wrap("dotnet")
                    .WithArguments($"{tempDir}/{project.Database}.Migrations.dll \"{connectionString}\"")
                    .WithStandardErrorPipe(PipeTarget.ToDelegate(Console.WriteLine))
                    .ExecuteAsync();

                AnsiConsole.MarkupLine($"[green]Executing {project.Database} migrations...done[/]");
            }

            foreach (var migrationProject in migrationProjects)
            {
                await ExecuteRunCommand(migrationProject);
            }
        }

        public IEnumerable<MigrationProject> GetFullMigrationPaths(DirectoryInfo workingDirectory)
        {
            var projects = _settings.MigrationProjectsToExecute ?? new Dictionary<string, string>();
            foreach (var (database, projectPath) in projects)
            {
                var fullProjectPath = Path.Combine(workingDirectory.FullName, projectPath);
                var file = new FileInfo(fullProjectPath);

                if (!file.Exists || file.Extension != ".csproj")
                    throw new Exception($"Invalid migration project file: {file.FullName}");

                yield return new MigrationProject(database, file.FullName);
            }
        }

        public void CleanupTemporaryFolders(DirectoryInfo workingDirectory)
        {
            foreach (var dir in workingDirectory.GetDirectories())
            {
                if (dir.Name.Contains("tmp_"))
                {
                    dir.Delete(true);
                }
            }
        }
    }

    public record MigrationProject(string Database, string FullPath);
}