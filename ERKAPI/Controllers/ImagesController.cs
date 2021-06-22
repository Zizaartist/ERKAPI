using Azure.Identity;
using Azure.Storage.Blobs;
using ERKAPI.Models;
using ERKAPI.Models.EnumModels;
using ERKAPI.StaticValues;
using ImageMagick;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERKAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ImagesController : Controller
    {
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly IConfiguration _configuration;
        private readonly ERKContext _context;

        public ImagesController(IWebHostEnvironment appEnvironment, IConfiguration configuration, ERKContext context)
        {
            _appEnvironment = appEnvironment;
            _configuration = configuration;
            _context = context;
        }

        // POST: api/Images/?useResize=True
        [HttpPost]
        public async Task<ActionResult<string>> PostImage(IFormFile uploadedFile, int? postId = null, bool isAvatar = false)
        {
            Post post = null;
            if (postId != null)
            {
                //Проверяем существование и право на создание
                var myId = this.GetMyId();

                post = _context.Posts.Include(p => p.PostData)
                                        .FirstOrDefault(p => p.PostId == postId);

                if (post == null)
                {
                    return NotFound();
                }
                if (post.AuthorId != myId)
                {
                    return Forbid();
                }
            }

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

            bool needsThumbnail = false;

            //Буфер для обработки файла без лишних действий (чтение, запись)
            using (var resultStream = new MemoryStream())
            {
                //Читаем из полученного файла
                using (var readStream = uploadedFile.OpenReadStream())
                {
                    using (var image = new MagickImage(readStream))
                    {
                        if (isAvatar)
                        {
                            CropAndResizeAvatar(image);
                        }
                        //Записываем в буфер
                        image.Write(resultStream);

                        resultStream.Position = 0; //Сбрасываем каретку, автоматически это не делается

                        //Компрессируем буфер, вместо файла
                        ImageOptimizer optimizer = new ImageOptimizer();
                        optimizer.Compress(resultStream);
                    }

                    readStream.Position = 0;

                    if (!isAvatar)
                    {
                        using (var thumbnailStream = new MemoryStream())
                        {
                            using (var thumbnailImage = new MagickImage(readStream))
                            {
                                if (thumbnailImage.Width > Constants.THUMBNAIL_WIDTH ||
                                    thumbnailImage.Height > Constants.THUMBNAIL_HEIGHT)
                                {
                                    needsThumbnail = true;

                                    CropAndResizeThumbnail(thumbnailImage);

                                    thumbnailImage.Write(thumbnailStream);

                                    thumbnailStream.Position = 0; //Сбрасываем каретку

                                    ImageOptimizer optimizer = new ImageOptimizer();
                                    optimizer.Compress(thumbnailStream);
                                }
                            }

                            //Отправляем поток файла-результата в облачное хранилище
                            if (needsThumbnail) await UploadToAzure("erkimages", "postimages/thumbnail" + newFileName, thumbnailStream);
                        }
                    }
                }

                //Отправляем поток файла-результата в облачное хранилище
                await UploadToAzure("erkimages", (isAvatar ? "avatars/" : "postimages/") + newFileName, resultStream);
            }

            if (postId != null)
            {
                var newImage = new PostMedia
                {
                    MediaType = MediaType.image,
                    Path = newFileName,
                    PreviewPath = ((needsThumbnail ? "thumbnail" : "") + newFileName)
                };
                post.PostData.PostMedia.Add(newImage);
                await _context.SaveChangesAsync();
            }

            return newFileName;
        }

        private void CropAndResizeAvatar(MagickImage image)
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

        private void CropAndResizeThumbnail(MagickImage image)
        {
            var rescaleSize = new MagickGeometry();
            rescaleSize.IgnoreAspectRatio = false;
            rescaleSize.Width = Constants.THUMBNAIL_WIDTH;

            image.Resize(rescaleSize);

            var cropSize = new MagickGeometry();
            cropSize.Width = Constants.THUMBNAIL_WIDTH;
            cropSize.Height = Constants.THUMBNAIL_HEIGHT;

            var difference = image.Height - Constants.THUMBNAIL_HEIGHT;
            //Если высота больше 480, то половину разницы использовать для смещения
            cropSize.Y = difference > 0 ? difference / 2 : 0;

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
