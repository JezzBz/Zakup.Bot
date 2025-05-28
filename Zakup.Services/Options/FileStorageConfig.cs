namespace Zakup.Services.Options;

public class FileStorageConfig
{
    public const string Key = "MinIO";
    public string Endpoint { get; set; }
    public string AccessKey { get; set; }
    public string SecretKey { get; set; }
    public bool UseSSL { get; set; }
    public string BucketName { get; set; }
}