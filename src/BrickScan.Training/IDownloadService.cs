using System.Threading.Tasks;

namespace BrickScan.Training
{
    public interface IDownloadService
    {
        Task DownloadAsync(string destinationDirectory);
    }
}
