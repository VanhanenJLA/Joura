using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using Microsoft.Playwright;
using Npgsql;
using Xunit;

namespace Joura.UiTests;

public sealed class TestAppFixture : IAsyncLifetime
{
    private readonly StringBuilder processOutput = new();
    private Process? appProcess;
    private IPlaywright? playwright;

    public string BaseUrl { get; private set; } = string.Empty;
    public string DatabaseName { get; private set; } = string.Empty;
    public string ConnectionString { get; private set; } = string.Empty;
    public IBrowser Browser { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        DatabaseName = $"joura_ui_{Guid.NewGuid():N}";
        ConnectionString = BuildDatabaseConnectionString(DatabaseName);
        await EnsureDatabaseExistsAsync(DatabaseName);

        var port = GetFreeTcpPort();
        BaseUrl = $"http://127.0.0.1:{port}";
        appProcess = StartApplicationProcess(port, ConnectionString);
        await WaitForApplicationAsync();

        playwright = await Playwright.CreateAsync();
        Browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = string.Equals(
                Environment.GetEnvironmentVariable("CI"),
                "true",
                StringComparison.OrdinalIgnoreCase),
            Timeout = 5000,
        });
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null)
        {
            await Browser.DisposeAsync();
        }

        playwright?.Dispose();

        if (appProcess is not null && !appProcess.HasExited)
        {
            appProcess.Kill(entireProcessTree: true);
            await appProcess.WaitForExitAsync();
        }

        await DropDatabaseIfExistsAsync(DatabaseName);
    }

    public async Task<IBrowserContext> CreateContextAsync()
    {
        return await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            AcceptDownloads = true,
            RecordVideoDir = Path.Combine(GetRepositoryRoot(), "artifacts", "playwright-videos"),
            RecordVideoSize = new RecordVideoSize
            {
                Width = 1280,
                Height = 720
            }
        });
    }

    private Process StartApplicationProcess(int port, string connectionString)
    {
        var isCi = string.Equals(
            Environment.GetEnvironmentVariable("CI"),
            "true",
            StringComparison.OrdinalIgnoreCase);
        var arguments = isCi
            ? "run --project src/Joura.Web --configuration Release --no-build --no-launch-profile"
            : "run --project src/Joura.Web --no-launch-profile";
        var startInfo = new ProcessStartInfo("dotnet", arguments)
        {
            WorkingDirectory = GetRepositoryRoot(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        startInfo.Environment["ASPNETCORE_URLS"] = $"http://127.0.0.1:{port}";
        startInfo.Environment["DatabaseProvider"] = "PostgreSql";
        startInfo.Environment["ConnectionStrings__PostgreSql"] = connectionString;
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";

        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                lock (processOutput)
                {
                    processOutput.AppendLine(args.Data);
                }
            }
        };

        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                lock (processOutput)
                {
                    processOutput.AppendLine(args.Data);
                }
            }
        };

        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start the Joura web application process for UI tests.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    private async Task WaitForApplicationAsync()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var deadline = DateTimeOffset.UtcNow.AddSeconds(90);

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (appProcess?.HasExited == true)
            {
                throw new InvalidOperationException(
                    $"The Joura web application exited before becoming ready.{Environment.NewLine}{GetProcessOutput()}");
            }

            try
            {
                using var response = await client.GetAsync($"{BaseUrl}/login");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return;
                }
            }
            catch
            {
            }

            await Task.Delay(500);
        }

        throw new TimeoutException(
            $"Timed out waiting for the Joura web application to start.{Environment.NewLine}{GetProcessOutput()}");
    }

    private static string BuildDatabaseConnectionString(string databaseName)
    {
        var configured = Environment.GetEnvironmentVariable("JOURA_UI_TEST_ADMIN_CONNECTION");
        var builder = string.IsNullOrWhiteSpace(configured)
            ? new NpgsqlConnectionStringBuilder
            {
                Host = "localhost",
                Port = 5432,
                Username = "joura",
                Password = "joura",
                Database = databaseName
            }
            : new NpgsqlConnectionStringBuilder(configured)
            {
                Database = databaseName
            };

        return builder.ConnectionString;
    }

    private static async Task EnsureDatabaseExistsAsync(string databaseName)
    {
        await using var connection = new NpgsqlConnection(BuildAdminConnectionString());

        try
        {
            await connection.OpenAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Could not connect to PostgreSQL for UI tests. Start the local database first, for example with 'docker compose up -d'.",
                ex);
        }

        await using var command = new NpgsqlCommand($"""CREATE DATABASE "{databaseName}" """, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DropDatabaseIfExistsAsync(string databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            return;
        }

        try
        {
            await using var connection = new NpgsqlConnection(BuildAdminConnectionString());
            await connection.OpenAsync();

            await using (var terminateCommand = new NpgsqlCommand(
                             """
                             SELECT pg_terminate_backend(pid)
                             FROM pg_stat_activity
                             WHERE datname = @databaseName AND pid <> pg_backend_pid();
                             """,
                             connection))
            {
                terminateCommand.Parameters.AddWithValue("databaseName", databaseName);
                await terminateCommand.ExecuteNonQueryAsync();
            }

            await using var dropCommand = new NpgsqlCommand($"""DROP DATABASE IF EXISTS "{databaseName}" """, connection);
            await dropCommand.ExecuteNonQueryAsync();
        }
        catch
        {
        }
    }

    private static string BuildAdminConnectionString()
    {
        var configured = Environment.GetEnvironmentVariable("JOURA_UI_TEST_ADMIN_CONNECTION");
        var builder = string.IsNullOrWhiteSpace(configured)
            ? new NpgsqlConnectionStringBuilder
            {
                Host = "localhost",
                Port = 5432,
                Username = "joura",
                Password = "joura",
                Database = "postgres"
            }
            : new NpgsqlConnectionStringBuilder(configured)
            {
                Database = "postgres"
            };

        return builder.ConnectionString;
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string GetRepositoryRoot()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    }

    private string GetProcessOutput()
    {
        lock (processOutput)
        {
            return processOutput.ToString();
        }
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class UiTestCollection : ICollectionFixture<TestAppFixture>
{
    public const string Name = "ui-tests";
}
