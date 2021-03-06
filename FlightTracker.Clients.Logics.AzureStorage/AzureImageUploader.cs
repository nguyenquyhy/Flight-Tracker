﻿using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlightTracker.Clients.Logics.AzureStorage
{
    public class AzureImageUploader : IImageUploader
    {
        private readonly CloudBlobContainer container;
        private readonly ILogger<AzureImageUploader> logger;

        public AzureImageUploader(IOptions<AppSettings> settings, ILogger<AzureImageUploader> logger)
        {
            var account = CloudStorageAccount.Parse(settings.Value.AzureStorage.ConnectionString);
            var client = account.CreateCloudBlobClient();
            container = client.GetContainerReference(settings.Value.AzureStorage.ContainerName);
            this.logger = logger;
        }

        private async Task InitializeAsync()
        {
            if (await container.CreateIfNotExistsAsync())
            {
                await container.SetPermissionsAsync(new BlobContainerPermissions
                {
                    PublicAccess = BlobContainerPublicAccessType.Blob
                });
            }
        }

        public async Task<List<string>> ListAsync()
        {
            await InitializeAsync();

            var result = new List<string>();
            while (true)
            {
                var response = await container.ListBlobsSegmentedAsync(null);
                result.AddRange(response.Results.Select(o => o.Uri.AbsoluteUri));
                if (response.ContinuationToken == null) break;
            }
            return result;
        }

        public async Task<string> UploadAsync(string name, byte[] image)
        {
            await InitializeAsync();

            var blob = container.GetBlockBlobReference(name);

            logger.LogDebug("Uploading image {name} of {length} bytes...", name, image.Length);
            await blob.UploadFromByteArrayAsync(image, 0, image.Length);
            logger.LogInformation("Uploaded image {name} of {length} bytes...", name, image.Length);
            return blob.Uri.AbsoluteUri;
        }

        public async Task DeleteAsync(string name)
        {
            await InitializeAsync();

            var blob = container.GetBlockBlobReference(name);
            await blob.DeleteIfExistsAsync();
        }
    }
}
