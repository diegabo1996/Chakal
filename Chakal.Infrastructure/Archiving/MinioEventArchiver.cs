using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Chakal.Core.Interfaces;
using Chakal.Core.Models.Events;
using Chakal.Infrastructure.Settings;

namespace Chakal.Infrastructure.Archiving
{
    /// <summary>
    /// Implementation of IEventArchiver that stores events in MinIO
    /// </summary>
    public class MinioEventArchiver : IEventArchiver
    {
        private readonly IMinioClient _minio;
        private readonly MinioSettings _cfg;
        private readonly JsonSerializerOptions _jsonOptions;

        public MinioEventArchiver(IMinioClient minio, IOptions<MinioSettings> cfg)
        {
            _minio = minio;
            _cfg = cfg.Value;
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            // Ensure bucket exists (fire and forget)
            _ = EnsureBucketAsync();
        }

        private async Task EnsureBucketAsync()
        {
            try
            {
                if (!await _minio.BucketExistsAsync(
                    new BucketExistsArgs().WithBucket(_cfg.BucketName)))
                {
                    await _minio.MakeBucketAsync(
                        new MakeBucketArgs().WithBucket(_cfg.BucketName));
                }
                
                // Set versioning enabled (not exposed in Minio .NET SDK 6.x)
                // In production, we would ensure versioning is enabled via minio console
            }
            catch (Exception)
            {
                // Log but don't throw - this ensures we don't block app startup
                // In production we would log this error
            }
        }

        public async ValueTask ArchiveAsync(WebcastEnvelope evt, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            // Format: raw/{yyyy}/{MM}/{dd}/{roomId}/{eventType}/{eventId}.jsonl.gz
            var key = $"raw/{now:yyyy}/{now:MM}/{now:dd}/{evt.RoomId}/{evt.EventType}/{evt.EventId}.jsonl.gz";

            await using var mem = new MemoryStream();
            await using (var gz = new GZipStream(mem, CompressionLevel.Fastest, leaveOpen: true))
            await using (var sw = new StreamWriter(gz))
            {
                await sw.WriteLineAsync(JsonSerializer.Serialize(evt, _jsonOptions));
            }
            
            mem.Seek(0, SeekOrigin.Begin);

            await _minio.PutObjectAsync(new PutObjectArgs()
                .WithBucket(_cfg.BucketName)
                .WithObject(key)
                .WithStreamData(mem)
                .WithObjectSize(mem.Length)
                .WithContentType("application/gzip"), ct);
        }
    }
} 