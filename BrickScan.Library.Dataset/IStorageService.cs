using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BrickScan.Library.Core;
using Microsoft.Extensions.Configuration;

namespace BrickScan.Library.Dataset
{
    public class LocalFileStorageService : IStorageService
    {
        private static readonly string[] _allowedExtensions = {".png", "jpg", "jpeg"};
        private readonly IConfiguration _configuration;
        
        public LocalFileStorageService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task<Uri> StoreImageAsync(ImageData imageData, string filenameWithoutExtension)
        {
            var ext = imageData.Format.ToFileExtension();

            if (!_allowedExtensions.Contains(ext))
            {
                throw new ArgumentException($"Unsupported image format ({ext}). " +
                                            $"Supported extensions are: {string.Join(",", _allowedExtensions)}.");
            }

            var directory = _configuration.GetValue<string>("StorageService:Directory");
            Directory.CreateDirectory(directory);
            var filePath = Path.Combine(directory, filenameWithoutExtension, ext);

            File.WriteAllBytes(filePath, imageData.RawBytes);

            return Task.FromResult(new Uri(filePath));
        }
    }

    public class AzureBlobStorageService : IStorageService
    {
        public Task<Uri> StoreImageAsync(ImageData imageData, string filenameWithoutExtension)
        {
            throw new NotImplementedException();
        }
    }

    public interface IStorageService
    {
        Task<Uri> StoreImageAsync(ImageData imageData, string filenameWithoutExtension);
    }
}
