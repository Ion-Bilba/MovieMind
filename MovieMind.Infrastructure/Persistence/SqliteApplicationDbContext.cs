using Microsoft.EntityFrameworkCore;

namespace MovieMind.Infrastructure.Persistence;

// SQLite maintains its own migration history and snapshot.
public class SqliteApplicationDbContext(DbContextOptions<SqliteApplicationDbContext> options)
    : ApplicationDbContext(options);
