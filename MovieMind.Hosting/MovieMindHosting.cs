using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using MovieMind.Application.Interfaces;
using MovieMind.Application.Services;
using MovieMind.Domain.Enums;
using MovieMind.Infrastructure.Persistence;
using MovieMind.Infrastructure.Security;

namespace MovieMind.Hosting;

public sealed record MovieMindStorage(string DataDirectory)
{
    public string PostersDirectory => Path.Combine(DataDirectory, "posters");
}

public static class MovieMindHosting
{
    public static void AddMovieMind(this WebApplicationBuilder builder)
    {
        var dataDirectory = Path.GetFullPath(builder.Configuration["Storage:DataDirectory"]
            ?? Path.Combine(builder.Environment.ContentRootPath, "..", "App_Data"));
        var storage = new MovieMindStorage(dataDirectory);
        Directory.CreateDirectory(storage.PostersDirectory);
        builder.Services.AddSingleton(storage);

        var provider = builder.Configuration["Database:Provider"] ?? "Sqlite";
        if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            builder.Services.AddDbContext<SqliteApplicationDbContext>(options => options.UseSqlite(
                builder.Configuration.GetConnectionString("Sqlite")
                ?? new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
                {
                    DataSource = Path.Combine(dataDirectory, "moviemind.db")
                }.ToString()));
            builder.Services.AddScoped<ApplicationDbContext>(services =>
                services.GetRequiredService<SqliteApplicationDbContext>());
        }
        else if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(
                builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Set ConnectionStrings__DefaultConnection for SQL Server.")));
        }
        else
        {
            throw new InvalidOperationException("Database:Provider must be Sqlite or SqlServer.");
        }

        builder.Services.AddScoped<IApplicationDbContext>(services =>
            services.GetRequiredService<ApplicationDbContext>());
        builder.Services.AddScoped<FilmCatalog>();

        var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
        var jwtOptions = jwtSection.Get<JwtOptions>()
            ?? throw new InvalidOperationException("Missing Jwt configuration.");
        if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) ||
            Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < 32)
        {
            throw new InvalidOperationException(
                "Configure Jwt:SigningKey in User Secrets or environment variables. Use at least 32 bytes.");
        }
        if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) ||
            string.IsNullOrWhiteSpace(jwtOptions.Audience) ||
            jwtOptions.AccessTokenMinutes is < 1 or > 1440)
        {
            throw new InvalidOperationException("Jwt configuration is invalid.");
        }

        builder.Services.Configure<JwtOptions>(jwtSection);
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IPasswordHashService, IdentityPasswordHashService>();
        builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        builder.Services.AddScoped<InitialAdminSeeder>();

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };
            });
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5104", "https://localhost:7142"];
        builder.Services.AddCors(options => options.AddPolicy("MovieMindWeb", policy =>
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));
        builder.Services.AddAuthorization();
        builder.Services.AddProblemDetails();
    }

    public static async Task InitializeMovieMindAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment() ||
            app.Services.GetService<MovieMindStorage>() is null)
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (db.Database.IsSqlite())
        {
            await db.Database.MigrateAsync();
        }

        await scope.ServiceProvider
            .GetRequiredService<InitialAdminSeeder>()
            .SeedAsync();
    }

    public static void MapMovieMind(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/films", async (FilmCatalog catalog, CancellationToken cancellationToken) =>
            (await catalog.Query.AsNoTracking().OrderByDescending(m => m.CreatedAt)
                .ToListAsync(cancellationToken)).Select(FilmDocument.From));

        api.MapGet("/films/{id:guid}", async (Guid id, FilmCatalog catalog, CancellationToken cancellationToken) =>
        {
            var movie = await catalog.Query.AsNoTracking().SingleOrDefaultAsync(m => m.Id == id, cancellationToken);
            return movie is null ? Results.NotFound() : Results.Ok(FilmDocument.From(movie));
        });

        api.MapPut("/films/{id:guid}", async (Guid id, FilmDocument input, FilmCatalog catalog,
            MovieMindStorage storage, CancellationToken cancellationToken) =>
        {
            var errors = new List<ValidationResult>();
            Validator.TryValidateObject(input, new ValidationContext(input), errors, validateAllProperties: true);
            if (id != input.Id) errors.Add(new("Film ID must match the URL.", [nameof(input.Id)]));
            if (FilmDocument.IsPosterPath(input.Poster) &&
                !File.Exists(Path.Combine(storage.PostersDirectory, Path.GetFileName(input.Poster))))
            {
                errors.Add(new("The uploaded poster is missing. Choose the image again.", [nameof(input.Poster)]));
            }
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors
                    .GroupBy(e => e.MemberNames.FirstOrDefault() ?? "film")
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage ?? "Invalid value.").ToArray()));
            }
            try
            {
                return Results.Ok(await catalog.SaveAsync(input, cancellationToken));
            }
            catch (DbUpdateException)
            {
                return Results.Problem("The film could not be saved. Reload the library and try again.", statusCode: 409);
            }
        }).RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin)));

        api.MapDelete("/films/{id:guid}", async (Guid id, ApplicationDbContext db, CancellationToken cancellationToken) =>
        {
            var movie = await db.Movies.FindAsync([id], cancellationToken);
            if (movie is null) return Results.NotFound();
            db.Movies.Remove(movie);
            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin)));

        api.MapPost("/posters", UploadPosterAsync)
            .DisableAntiforgery()
            .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin)));

        app.MapGet("/media/posters/{name}", (string name, MovieMindStorage storage) =>
        {
            if (!FilmDocument.IsPosterPath($"/media/posters/{name}")) return Results.NotFound();
            var path = Path.Combine(storage.PostersDirectory, name);
            if (!File.Exists(path)) return Results.NotFound();
            var contentType = Path.GetExtension(name) switch
            {
                ".jpg" => "image/jpeg", ".png" => "image/png", _ => "image/webp"
            };
            return Results.File(path, contentType, enableRangeProcessing: true);
        });
    }

    private static async Task<IResult> UploadPosterAsync(HttpRequest request, MovieMindStorage storage,
        CancellationToken cancellationToken)
    {
        const long maxBytes = 10 * 1024 * 1024;
        if (request.ContentLength > maxBytes + 64 * 1024)
            return Results.Problem("Choose an image up to 10 MB.", statusCode: 413);
        if (!request.HasFormContentType) return Results.BadRequest(new { message = "Choose an image file." });
        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");
        if (file is null || file.Length == 0) return Results.BadRequest(new { message = "Choose a non-empty image." });
        if (file.Length > maxBytes) return Results.Problem("Choose an image up to 10 MB.", statusCode: 413);
        await using var source = file.OpenReadStream();
        var header = new byte[12];
        var count = await source.ReadAtLeastAsync(header, 12, throwOnEndOfStream: false, cancellationToken);
        var extension = count >= 3 && header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff ? ".jpg"
            : count >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ? ".png"
            : count == 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8) ? ".webp" : null;
        if (extension is null) return Results.BadRequest(new { message = "Choose a JPG, PNG or WebP image." });
        var name = Guid.NewGuid().ToString("N") + extension;
        var destination = Path.Combine(storage.PostersDirectory, name);
        try
        {
            await using var target = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await target.WriteAsync(header.AsMemory(0, count), cancellationToken);
            await source.CopyToAsync(target, cancellationToken);
        }
        catch
        {
            File.Delete(destination);
            throw;
        }
        return Results.Ok(new { url = $"/media/posters/{name}" });
    }
}