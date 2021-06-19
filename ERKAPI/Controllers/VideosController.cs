using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Azure.Management.Media;
using ERKAPI.Controllers.FrequentlyUsed;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Rest;
using Microsoft.Identity.Client;
using Microsoft.Rest.Azure.Authentication;
using Microsoft.Azure.Management.Media.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using ERKAPI.Models;
using ERKAPI.StaticValues;
using ERKAPI.Models.EnumModels;
using System.Text;

namespace ERKAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class VideosController : Controller
    {
        private static readonly FormOptions _defaultFormOptions = new FormOptions();
        private readonly long _fileSizeLimit = 100000000000;
        //Костыли
        private string fileName;
        private string extension;

        private readonly IWebHostEnvironment _appEnvironment;
        private readonly IConfiguration _configuration;
        private readonly ConfigWrapper _configWrapper;
        private readonly ERKContext _context;

        public VideosController(IWebHostEnvironment appEnvironment, IConfiguration configuration, ConfigWrapper configWrapper, ERKContext context)
        {
            _appEnvironment = appEnvironment;
            _configuration = configuration;
            _configWrapper = configWrapper;
            _context = context;
        }

        //Сам не знаю как работает, будем разжевывать
        //Нужно отправлять в form-data значение id медиа файла в ключ postId и лишь потом сам файл
        [HttpPost]
        [DisableRequestSizeLimit]
        public async Task<ActionResult> UploadFile()
        {
            Post post = null;

            //Какая-то проверка
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                ModelState.AddModelError("File",
                    $"The request couldn't be processed (Error 1).");
                // Log error

                return BadRequest(ModelState);
            }

            //Проверяем размер и тип файла из content header-a запроса
            var boundary = MultipartRequestHelper.GetBoundary(
                MediaTypeHeaderValue.Parse(Request.ContentType),
                _defaultFormOptions.MultipartBoundaryLengthLimit);
            //Создаем читателя и читаем 1й фрагмент
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);
            var section = await reader.ReadNextSectionAsync();

            List<string> urls = new List<string>();

            //Читаем пока не кончится
            while (section != null)
            {
                var hasContentDispositionHeader =
                    ContentDispositionHeaderValue.TryParse(
                        section.ContentDisposition, out var contentDisposition);

                if (hasContentDispositionHeader)
                {
                    if (!MultipartRequestHelper
                        .HasFileContentDisposition(contentDisposition))
                    {
                        if (contentDisposition.Name.Value == "postId")
                        {
                            int postId = -1;
                            using (var memoryStream = new MemoryStream())
                            {
                                await section.Body.CopyToAsync(memoryStream);
                                postId = int.Parse(Encoding.UTF8.GetString(memoryStream.ToArray()));
                            }

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
                        else
                        {
                            ModelState.AddModelError("File",
                                $"The request couldn't be processed (Error 2).");
                            // Log error

                            return BadRequest(ModelState);
                        }
                    }
                    else
                    {
                        var trustedFileNameForDisplay = WebUtility.HtmlEncode(
                                contentDisposition.FileName.Value);
                        extension = Path.GetExtension(contentDisposition.FileName.Value).ToLowerInvariant();
                        var trustedFileNameForFileStorage = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + extension; //Удобно, уже предусмотрели :)
                        fileName = trustedFileNameForFileStorage;

                        var streamedFileContent = await FileHelpers.ProcessStreamedFile(
                            section, contentDisposition, ModelState, _fileSizeLimit);

                        ModelState.Values.SelectMany(e => e.Errors).ToList().ForEach(e => Debug.WriteLine(e.ErrorMessage));
                        if (!ModelState.IsValid)
                        {
                            return BadRequest(ModelState);
                        }

                        //Отправляем поток файла-результата в облачное хранилище
                        using (var memStream = new MemoryStream(streamedFileContent))
                        {
                            await AzureUpload(trustedFileNameForFileStorage, memStream, post);
                        }
                    }
                }

                section = await reader.ReadNextSectionAsync();
            }

            return Ok();
        }

        private async Task AzureUpload(string fileName, MemoryStream stream, Post post)
        {
            //Временно устанавливаем тип медиа на "изображение" и ставим готовую картинку
            var newMediaFile = new PostMedia
            {
                MediaType = MediaType.image,
                PreviewPath = Constants.VIDEO_MISSING_IMAGE,
                Path = Constants.VIDEO_MISSING_IMAGE
            };
            post.PostData.PostMedia.Add(newMediaFile);
            _context.SaveChanges();

            //Отправляем полученный файл в blob
            string resourceGroup = _configuration["ResourceGroup"];
            string accountName = _configuration["AccountName"];
            string transformName = _configuration["VideoEncoderName"];

            try
            {
                var client = await AzureHelper.CreateMediaServicesClientAsync(_configWrapper);

                var inputAsset = await AzureHelper.CreateInputAssetAsync(client, resourceGroup, accountName, fileName, stream);
                var outputAsset = await AzureHelper.CreateOutputAssetAsync(client, resourceGroup, accountName, fileName);

                var transform = await AzureHelper.GetOrCreateTransformAsync(client, resourceGroup, accountName, transformName);

                await AzureHelper.SubmitJobAsync(client, resourceGroup, accountName, transform.Name, $"{fileName}Encoding", inputAsset.Name, outputAsset.Name, newMediaFile.PostMediaId);
            }
            //Если произошла ошибка - удалить болванку
            catch 
            {
                _context.PostMedia.Remove(newMediaFile);
                _context.SaveChanges();
            }
        }
    }
}
