namespace Chakal.Infrastructure.Settings
{
    /// <summary>
    /// Configuration settings for Minio S3 compatible storage
    /// </summary>
    public record MinioSettings
    {
        /// <summary>
        /// S3-compatible endpoint URL without protocol
        /// </summary>
        public string Endpoint { get; set; } = "192.168.1.230:9100";
        
        /// <summary>
        /// S3 access key
        /// </summary>
        public string AccessKey { get; set; } = "vFpdN5CJGjY5o0jPKVww";
        
        /// <summary>
        /// S3 secret key
        /// </summary>
        public string SecretKey { get; set; } = "5KpII0ID5oOqFOmSR8G6rd7HJJNBFb9eD1Xb3dTB";
        
        /// <summary>
        /// S3 bucket name for raw events
        /// </summary>
        public string BucketName { get; set; } = "dev-chakal-raw";
        
        /// <summary>
        /// Whether to use SSL for S3 connections
        /// </summary>
        public bool UseSSL { get; set; } = false;
    }
} 