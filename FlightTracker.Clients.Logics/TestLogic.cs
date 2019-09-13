using System;
using System.Threading.Tasks;

namespace FlightTracker.Clients.Logics
{
    public class TestLogic
    {
        private readonly IImageUploader imageUploader;

        public TestLogic(
            IImageUploader imageUploader)
        {
            this.imageUploader = imageUploader;
        }

        public async Task TestImageUploader()
        {
            var url = await imageUploader.UploadAsync("test.txt", new byte[] { 48, 49, 50 });
            var urls = await imageUploader.ListAsync();
            if (!urls.Contains(url)) throw new Exception("Cannot see uploaded file!");
            await imageUploader.DeleteAsync("test.txt");
        }
    }
}
