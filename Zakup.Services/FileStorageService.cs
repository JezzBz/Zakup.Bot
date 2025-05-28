using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Telegram.Bot.Types;
using Zakup.Services.Options;

namespace Zakup.Services;

public class FileStorageService
{
    private readonly MinioClient _client;
    private readonly FileStorageConfig _config;
    
    public FileStorageService(MinioClient client, IOptions<FileStorageConfig> config)
    {
        _client = client;
        _config = config.Value;
    }
    
    public async Task UploadFile(MemoryStream stream, string fileName, string fileType)
    {
        stream.Position = 0;
        // Проверяем бакет
        var bucketExists = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(_config.BucketName));
        if (!bucketExists)
        {
            await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_config.BucketName));
        }
    
        // Загружаем файл
        await _client.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_config.BucketName)
            .WithObject(fileName)
            .WithStreamData(stream)
            .WithObjectSize(stream.Length)
            .WithContentType(fileType));
    }
    
    public async Task<Stream> GetFile(string fileName)
    {
        var minio = new MinioClient()
            .WithEndpoint(_config.Endpoint)
            .WithCredentials(_config.AccessKey, _config.SecretKey)
            .WithSSL(_config.UseSSL)
            .Build();
    
        var stream = new MemoryStream();
        await minio.GetObjectAsync(new GetObjectArgs()
            .WithBucket(_config.BucketName)
            .WithObject(fileName)
            .WithCallbackStream(data => data.CopyTo(stream)));
    
        stream.Position = 0;
        return stream;
    }
}