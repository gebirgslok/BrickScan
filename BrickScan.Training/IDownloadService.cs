using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BrickScan.Training
{
    public class DownloadService : IDownloadService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DownloadService> _logger;
        private readonly IConfiguration _configuration;

        public DownloadService(IHttpClientFactory httpClientFactory, 
            ILogger<DownloadService> logger, 
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task DownloadAsync()
        {
            //TODO: Query class training images
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(_configuration["BrickScanApiBaseUrl"]);

            var response = await httpClient.GetAsync("dataset/classes/training-images");

            //TODO: Download images to local drive
            await Task.Delay(1000);
        }
    }

    public interface IDownloadService
    {
        Task DownloadAsync();
    }
}
