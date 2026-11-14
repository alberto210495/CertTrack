using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace CertTrack.Api.Tests;

// Starts the real API in memory, with its own empty database.
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString = $"Data Source=certtrack-tests-{Guid.NewGuid()};Mode=Memory;Cache=Shared";
    private SqliteConnection? _keepAlive;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // An in-memory database is deleted when its last connection closes, so keep one open.
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();

        // Development makes the API create a temporary JWT key, so the tests need no secret.
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Default", _connectionString);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _keepAlive?.Dispose();
    }
}
