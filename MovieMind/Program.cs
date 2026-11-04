using Microsoft.EntityFrameworkCore;
using MovieMind.Application.Interfaces;
using MovieMind.Application.Services;
using MovieMind.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddScoped<IApplicationDbContext>(provider =>
    provider.GetRequiredService<ApplicationDbContext>());


builder.Services.AddScoped<IGenreService, GenreService>();
builder.Services.AddScoped<IActorService, ActorService>();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/openapi/v1.json", "MovieMind API"));
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();