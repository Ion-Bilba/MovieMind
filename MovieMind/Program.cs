using MovieMind.Application.Interfaces;
using MovieMind.Application.Services;
using MovieMind.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.AddMovieMind();
builder.Services.AddScoped<IGenreService, GenreService>();
builder.Services.AddScoped<IActorService, ActorService>();
builder.Services.AddScoped<IMovieService, MovieService>();

var app = builder.Build();
await app.InitializeMovieMindAsync();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/openapi/v1.json", "MovieMind API"));
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("MovieMindWeb");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapMovieMind();
app.Run();