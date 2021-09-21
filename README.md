# Test MSSQL Database 

This is a template used to manage a test MSSQL database that needs to pull and load schema from an existing database as well as possibly load FluentMigration schemas.

## Tools
- Pull schema for remote database objects into local SQL files that can be loaded into the test database using [mssql-scripter](https://github.com/Microsoft/mssql-scripter). Executed within an automatically built Python Docker container.
- Create, reset, or destroy a local test MSSQL database using a Docker container.
- Execute SQL files defining DB schema directly against the local test database.

## Configuration
Configuration is broken down into sections by function the tool needs to manage within `appsettings.json`. File is pre-populated with examples.

### Initializing database settings
- **MssqlDockerImage**: This is the Docker image to use for the MSSQL database.
- **ContainerName**: Your desired name for the MSSQL DB container.
- **Port**: Local port to bind the database to.
- **DatabasePassword**: 'sa' login password for the database. Use MS recommended secure password practices.
- **HealthCheckTimeoutInSeconds**: Number of seconds to wait for MSSQL DB container to be up before timeout.

### Loading existing DB schema settings
- **SqlSourceFolderRelativeToWorkingDirectory**: Directory containing stored SQL files to execute relative to the working directory (customizable at runtime).
- **DatabasesToCreate**: Array of database names to be created.
- **SqlFilesToExecute**: Array of SQL filenames contained within the SQL source directory that will be executed against the server (can be left empty).
- **MigrationProjectsToExecute**: Array of .NET project `csproj` paths for FluentMigration projects that will be executed (can be left empty).

### SchemaPullSettings
- **TimeoutInSeconds**: Number of seconds to wait for `mssql-scripter` to execute before timeout (recommended to increase as the number of DB objects being pulled increases).
- **PythonImageTag**: Python Docker image to use for executing `mssql-scripter`.
- **Username**: Username to log into remote database for schema-pull.
- **Password**: Password to log into remote database for schema-pull.
- **CustomPythonImageName**: Name for the created Python container.
- **MssqlScripterFlags**: Flags to use when executing `mssql-scripter`. See [mssql-scripter docs](https://github.com/microsoft/mssql-scripter/blob/dev/doc/usage_guide.md#options).
- **Sources**: Array of objects defining a source remote database to pull DB schema from:
    - **Host**: Hostname/address for the remote database.
    - **Database**: Name of the database to script SQL objects from.
    - **Objects**: Array of SQL object names on the remote database to generate SQL for.

## Usage
```
dotnet run [./path/to/TestEnvironmentTool.csproj] -- [COMMAND] [OPTIONS]

# For additional information:
dotnet run [./path/to/TestEnvironmentTool.csproj] -- --help
```

### Commands
#### `pull-schema`
Pull schema for remote database objects based on configuration. This will first create a Python docker container then connect to the remote database and script configured objects, outputting the SQL into the `./sql` folder within the working directory. These files will be formatted as `DatabaseName-SchemaPull.sql`. Existing schema pull files are also backed up within `./sql/backups` each time this command runs.

**OPTIONS**

-w Specify working directory (`./` by default)

**EXAMPLE**:

```
dotnet run -- pull-schema -w ../
```

#### `reset`
Re-initializes test MSSQL database by executing all other commands, optionally including `pull-schema`. This can be used at any time and will first destroy any existing container, then create a new one and load appropriate SQL into the database.

**OPTIONS**

-w Specify working directory (`./` by default)

-p Include schema pull before loading SQL

**EXAMPLE**:

```
dotnet run -- reset -p -w ../
```

#### `init`
Initializes the Docker MSSQL database based on configuration. This will only create a container and start it. It will not create a new container if one already exists.

**OPTIONS**

*None*

**EXAMPLE**:

```
dotnet run -- init
```

#### `destroy`
Destroys any existing Docker MSSQL container with the name provided in configuration.

**OPTIONS**

*None*

**EXAMPLE**:

```
dotnet run -- destroy
```

#### `load-schema`
Creates all databases listed in the configuration. Then loads all SQL files against the database specified in configuration. Finally, it executes any FluentMigration projects configured. Run this after the `init` command if executing manually.

**OPTIONS**

-w Specify working directory (`./` by default)

**EXAMPLE**:

```
dotnet run -- pull-schema -w ../
```



