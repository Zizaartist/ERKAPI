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

        public VideosController(IWebHostEnvironment appEnvironment, IConfiguration configuration, ConfigWrapper configWrapper)
        {
            _appEnvironment = appEnvironment;
            _configuration = configuration;
            _configWrapper = configWrapper;
        }

        //Сам не знаю как работает, будем разжевывать
        [HttpPost]
        [DisableRequestSizeLimit]
        public async Task<ActionResult<string>> UploadFile()
        {
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

            string url = null;

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
                        ModelState.AddModelError("File",
                            $"The request couldn't be processed (Error 2).");
                        // Log error

                        return BadRequest(ModelState);
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
                            url = await AzureUpload(trustedFileNameForFileStorage, memStream);
                        }
                    }
                }

                section = await reader.ReadNextSectionAsync();
            }

            return url;
        }

        [Route("asd")]
        [HttpGet]
        public async Task<ActionResult> Test() 
        {
            await AzureUpload("testasdqwe", null);

            return Ok();
        }

        private async Task<string> AzureUpload(string fileName, MemoryStream stream) 
        {
            //Отправляем полученный файл в blob
            string resourceGroup = _configuration["ResourceGroup"];
            string accountName = _configuration["AccountName"];
            string transformName = _configuration["VideoEncoderName"];

            var client = await AzureHelper.CreateMediaServicesClientAsync(_configWrapper);

            var inputAsset = await AzureHelper.CreateInputAssetAsync(client, resourceGroup, accountName, fileName, stream);
            var outputAsset = await AzureHelper.CreateOutputAssetAsync(client, resourceGroup, accountName, fileName);

            var transform = await AzureHelper.GetOrCreateTransformAsync(client, resourceGroup, accountName, transformName);

            var encodingJob = await AzureHelper.SubmitJobAsync(client, resourceGroup, accountName, transform.Name, $"{fileName}Encoding", inputAsset.Name, outputAsset.Name);
            var encodingResult = await AzureHelper.WaitForJobToFinishAsync(client, resourceGroup, accountName, transform.Name, encodingJob.Name);

            var streamingLocator = await AzureHelper.CreateStreamingLocatorAsync(client, resourceGroup, accountName, outputAsset.Name, $"{fileName}Locator");
            var url = await AzureHelper.GetStreamingUrlAsync(client, resourceGroup, accountName, streamingLocator.Name);

            await AzureHelper.CleanUpAsync(client, resourceGroup, accountName, transform.Name, encodingJob.Name, inputAsset.Name, null);

            return url;
        }
    }
}
