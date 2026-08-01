using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebApiApp.Models;

namespace WebApiApp.Services;

/// <summary>
/// Cliente para a Vercel Blob Storage API.
/// Não existe SDK oficial da Vercel para .NET (apenas JS, Python e a CLI),
/// então este serviço chama a API REST diretamente, no mesmo formato usado
/// internamente pelo pacote @vercel/blob e pela Vercel CLI.
/// Referência: https://vercel.com/docs/vercel-blob
/// Se algum endpoint parar de funcionar, valide o comportamento esperado
/// rodando o comando equivalente com `vercel blob` (CLI) para comparar.
/// </summary>
public class VercelBlobService
{
    private const string BaseUrl = "https://blob.vercel-storage.com";
    private const string Prefix = "pdfs/";

    private readonly HttpClient _http;
    private readonly ILogger<VercelBlobService> _logger;

    public VercelBlobService(HttpClient http, IConfiguration config, ILogger<VercelBlobService> logger)
    {
        _logger = logger;

        var token = config["BLOB_READ_WRITE_TOKEN"]
            ?? Environment.GetEnvironmentVariable("BLOB_READ_WRITE_TOKEN")
            ?? throw new InvalidOperationException(
                "A variável BLOB_READ_WRITE_TOKEN não está configurada. " +
                "Crie um Blob Store no dashboard da Vercel e copie o token para as variáveis de ambiente do projeto.");

        _http = http;
        _http.BaseAddress = new Uri(BaseUrl);
        _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        _http.DefaultRequestHeaders.Add("x-api-version", "7");
    }

    public async Task<BlobFile> UploadAsync(IFormFile file)
    {
        var safeFileName = Path.GetFileName(file.FileName);
        var pathname = $"{Prefix}{Guid.NewGuid()}-{safeFileName}";

        using var content = new StreamContent(file.OpenReadStream());
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");

        using var request = new HttpRequestMessage(HttpMethod.Put, pathname)
        {
            Content = content
        };
        request.Headers.Add("x-content-type", "application/pdf");
        request.Headers.Add("x-add-random-suffix", "0");

        var response = await _http.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Falha ao enviar para a Vercel Blob: {Status} {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"Falha ao enviar o arquivo para a Vercel Blob ({(int)response.StatusCode}).");
        }

        var result = JsonSerializer.Deserialize<PutResponse>(body, JsonOptions)
            ?? throw new InvalidOperationException("Resposta inesperada da Vercel Blob ao enviar o arquivo.");

        return new BlobFile(
            result.Pathname,
            safeFileName,
            file.Length,
            DateTime.UtcNow,
            result.Url,
            result.DownloadUrl);
    }

    public async Task<List<BlobFile>> ListAsync()
    {
        var response = await _http.GetAsync($"?prefix={Uri.EscapeDataString(Prefix)}&limit=1000");
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Falha ao listar arquivos na Vercel Blob: {Status} {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"Falha ao listar os arquivos na Vercel Blob ({(int)response.StatusCode}).");
        }

        var result = JsonSerializer.Deserialize<ListResponse>(body, JsonOptions)
            ?? new ListResponse(new List<ListedBlob>());

        return result.Blobs
            .OrderByDescending(b => b.UploadedAt)
            .Select(b => new BlobFile(
                b.Pathname,
                ExtractOriginalFileName(b.Pathname),
                b.Size,
                b.UploadedAt,
                b.Url,
                b.DownloadUrl))
            .ToList();
    }

    public async Task<bool> DeleteAsync(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "delete")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { urls = new[] { url } }),
                Encoding.UTF8,
                "application/json")
        };

        var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    private static string ExtractOriginalFileName(string pathname)
    {
        // pathname tem o formato: pdfs/{guid}-{nome-original}.pdf
        var fileName = Path.GetFileName(pathname);
        var dashIndex = fileName.IndexOf('-');
        return dashIndex >= 0 && dashIndex < fileName.Length - 1
            ? fileName[(dashIndex + 1)..]
            : fileName;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private record PutResponse(
        [property: JsonPropertyName("pathname")] string Pathname,
        [property: JsonPropertyName("url")] string Url,
        [property: JsonPropertyName("downloadUrl")] string DownloadUrl);

    private record ListResponse(
        [property: JsonPropertyName("blobs")] List<ListedBlob> Blobs);

    private record ListedBlob(
        [property: JsonPropertyName("pathname")] string Pathname,
        [property: JsonPropertyName("url")] string Url,
        [property: JsonPropertyName("downloadUrl")] string DownloadUrl,
        [property: JsonPropertyName("size")] long Size,
        [property: JsonPropertyName("uploadedAt")] DateTime UploadedAt);
}
