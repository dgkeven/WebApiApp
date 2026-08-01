using WebApiApp.Models;
using WebApiApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient<VercelBlobService>();

// Permite uploads de até 20 MB (ajuste conforme necessário).
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 20 * 1024 * 1024;
});

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseDefaultFiles();
app.UseStaticFiles();

var summaries = new[]
{
    "Gelado", "Fresco", "Ameno", "Agradável", "Quente", "Muito quente", "Escaldante"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-5, 40),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapPost("/files/upload", async (IFormFile file, VercelBlobService blob) =>
{
    if (file.Length == 0)
    {
        return Results.BadRequest("Arquivo vazio.");
    }

    if (Path.GetExtension(file.FileName).ToLowerInvariant() != ".pdf")
    {
        return Results.BadRequest("Apenas arquivos PDF são permitidos.");
    }

    var uploaded = await blob.UploadAsync(file);
    return Results.Ok(uploaded);
})
.DisableAntiforgery()
.WithName("UploadPdf");

app.MapGet("/files", async (VercelBlobService blob) => await blob.ListAsync())
.WithName("ListFiles");

app.MapDelete("/files", async (string url, VercelBlobService blob) =>
    await blob.DeleteAsync(url) ? Results.NoContent() : Results.NotFound())
.WithName("DeleteFile");

app.MapGet("/api-info", () => "API rodando! Acesse /swagger para a documentação ou / para a página de upload.");

app.Run();
