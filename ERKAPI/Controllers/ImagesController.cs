using Azure.Identity;
using Azure.Storage.Blobs;
using ERKAPI.StaticValues;
using ImageMagick;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ERKAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ImagesController : ControllerBase
    {
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly IConfiguration _configuration;
        private readonly BlobServiceClient _blobServiceClient;

        public ImagesController(IWebHostEnvironment appEnvironment, IConfiguration configuration, BlobServiceClient blobServiceClient)
        {
            _appEnvironment = appEnvironment;
            _configuration = configuration;
            _blobServiceClient = blobServiceClient;
        }

        // POST: api/Images/?useResize=True
        [HttpPost]
        public async Task<ActionResult<string>> PostImage(IFormFile uploadedFile, bool useCrop = false)
        {
            if (uploadedFile == null)
            {
                return BadRequest();
            }

            //Проверить все ли являются изображениями и имеют реальный размер
            if (uploadedFile.Length <= 0)
            {
                return BadRequest(); //probably wrong code
            }
            if (!uploadedFile.ContentType.Contains("image"))
            {
                return BadRequest(); //probably wrong code    
            }

            // путь к папке Files, ЗАМЕНИТЬ Path.GetTempFileName на более надежный генератор
            string newFileName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + ".jpg";

            //Буфер для обработки файла без лишних действий (чтение, запись)
            using (var resultStream = new MemoryStream())
            {
                //Читаем из полученного файла
                using (var readStream = uploadedFile.OpenReadStream())
                {
                    using (var image = new MagickImage(readStream))
                    {
                        if (useCrop)
                        {
                            CropAndResize(image);
                        }
                        //Записываем в буфер
                        image.Write(resultStream);

                        resultStream.Position = 0; //Сбрасываем каретку, автоматически это не делается

                        //Компрессируем буфер, вместо файла
                        ImageOptimizer optimizer = new ImageOptimizer();
                        optimizer.Compress(resultStream);
                    }
                }
                //Отправляем поток файла-результата в облачное хранилище
                await UploadToAzure("erkimages", (useCrop ? "avatars/" : "postimages/") + newFileName, resultStream);
            }

            return newFileName;
        }

        private void CropAndResize(MagickImage image)
        {
            int maxSize;
            MagickGeometry rescaleSize;
            MagickGeometry cropSize;
            int xOffset;
            int yOffset;

            if (image.Height >= image.Width)
            {
                rescaleSize = new MagickGeometry();
                rescaleSize.IgnoreAspectRatio = false;
                maxSize = image.Width <= Constants.MAX_SIZE ? image.Width : Constants.MAX_SIZE;
                rescaleSize.Width = maxSize;

                image.Resize(rescaleSize);

                xOffset = 0;
                yOffset = image.Height / 2 - maxSize / 2;
            }
            else
            {
                rescaleSize = new MagickGeometry();
                rescaleSize.IgnoreAspectRatio = false;
                maxSize = image.Height <= Constants.MAX_SIZE ? image.Height : Constants.MAX_SIZE;
                rescaleSize.Height = maxSize;

                image.Resize(rescaleSize);

                xOffset = image.Width / 2 - maxSize / 2;
                yOffset = 0;
            }

            cropSize = new MagickGeometry(xOffset, yOffset, maxSize, maxSize);

            image.Crop(cropSize);
            image.Format = MagickFormat.Jpg;
        }

        private async Task UploadToAzure(string containerName, string blobName, MemoryStream stream)
        {
            // Construct the blob container endpoint from the arguments.
            string containerEndpoint = string.Format("https://{0}.blob.core.windows.net/{1}",
                                                        _configuration["BlobStorageName"],
                                                        containerName);

            // Get a credential and create a client object for the blob container.
            BlobContainerClient containerClient = new BlobContainerClient(new Uri(containerEndpoint),
                                                new DefaultAzureCredential());

            try
            {
                await containerClient.UploadBlobAsync(blobName, stream);
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
