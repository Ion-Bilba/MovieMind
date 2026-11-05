using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MovieMind.Infrastructure.Persistence;

namespace MovieMind.API;

public sealed class SqlServerDesignFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=localhost;Database=MovieMindDb;Trusted_Connection=True;TrustServerCertificate=True").Options);
}

public sealed class SqliteDesignFactory : IDesignTimeDbContextFactory<SqliteApplicationDbContext>
{
    public SqliteApplicationDbContext CreateDbContext(string[] args)
    {
        // Run EF commands from the repository root, as documented in README.md.
        var directory = Path.GetFullPath(Environment.GetEnvironmentVariable("Storage__DataDirectory") ?? "App_Data");
        Directory.CreateDirectory(directory);
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Sqlite")
            ?? new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
            {
                DataSource = Path.Combine(directory, "moviemind.db")
            }.ToString();
        return new(new DbContextOptionsBuilder<SqliteApplicationDbContext>().UseSqlite(connection).Options);
    }
}
