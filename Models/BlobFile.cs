namespace WebApiApp.Models;

public record BlobFile(
    string Pathname,
    string FileName,
    long SizeInBytes,
    DateTime UploadedAt,
    string Url,
    string DownloadUrl);
