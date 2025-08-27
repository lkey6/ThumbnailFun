using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ThumbnailFunction
{
    public static class GenerateThumbnail
    {
        [FunctionName("GenerateThumbnail")]
        public static async Task Run(
            [BlobTrigger("mengmeng/{name}", Connection = "AzureWebJobsStorage")] Stream inputBlob,
            string name,
            ILogger log)
        {
            try
            {
                log.LogInformation($"Blob trigger fired: {name}, size: {inputBlob.Length} bytes");

                using Image image = await Image.LoadAsync(inputBlob);

                int width = 150;
                int height = 150;
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(width, height),
                    Mode = ResizeMode.Max
                }));

                using MemoryStream outputStream = new MemoryStream();
                await image.SaveAsJpegAsync(outputStream);
                outputStream.Position = 0;

                string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                BlobContainerClient containerClient = new BlobContainerClient(connectionString, "thumbnails");

                await containerClient.CreateIfNotExistsAsync();
                log.LogInformation("Thumbnails container checked/created.");

                string blobName = name.Replace("\\", "/");
                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                await blobClient.UploadAsync(outputStream, overwrite: true);
                log.LogInformation($"Thumbnail successfully created for blob: {name}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Error generating thumbnail for blob: {name}");
            }
        }
    }
}
