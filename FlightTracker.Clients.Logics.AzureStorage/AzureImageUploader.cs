using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace FlightTracker.Clients.Logics.AzureStorage
{
    public class AzureImageUploader : IImageUploader
    {
        private readonly CloudBlobContainer container;

        public AzureImageUploader(IOptions<AppSettings> settings)
        {
            var account = CloudStorageAccount.Parse(settings.Value.AzureStorage.ConnectionString);
            var client = account.CreateCloudBlobClient();
            container = client.GetContainerReference(settings.Value.AzureStorage.ContainerName);
        }

        public async Task<string> UploadAsync(string name, byte[] image)
        {
            var blob = container.GetBlockBlobReference(name);
            await blob.UploadFromByteArrayAsync(image, 0, image.Length);
            return blob.Uri.AbsoluteUri;
        }
    }
}
