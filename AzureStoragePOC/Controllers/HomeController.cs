using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AzureStoragePOC.Models;
using System.IO;
using Microsoft.AspNetCore.Http;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace AzureStoragePOC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private async Task<BlobContainerClient> StartAzureConnection()
        {
            string connectionString ="DefaultEndpointsProtocol=https;AccountName=azurepoc1993;AccountKey=GeDcGm2eVpVOwIoJxhd28Q5tbE3LYOqK+LOsSBoykLk6dnvq9jkwHeGCCgKFLxgeLryzdTaM5WDJEV6pIOakuQ==;EndpointSuffix=core.windows.net";
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);

            //Create a unique name for the container
            string containerName = "poc4";
            // Create the container and return a container client object
            BlobContainerClient containerClient;
            BlobContainerItem container =  blobServiceClient.GetBlobContainers().Where(b=>b.Name== containerName).FirstOrDefault();
            if (container == null)
            {
                containerClient = await blobServiceClient.CreateBlobContainerAsync(containerName,PublicAccessType.Blob);
            }
            else
            {
                containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            }
            return containerClient;
        }

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            var containerClient = await StartAzureConnection();
            if (file == null || file.Length == 0)
                return Content("file not selected");

            BlobClient blobClient = containerClient.GetBlobClient(file.FileName);
           using Stream uploadFileStream = file.OpenReadStream();
            var blobInfo =await blobClient.UploadAsync(uploadFileStream,true);
            uploadFileStream.Close();

            return RedirectToAction("Files");
        }

        public async Task<IActionResult> Files()
        {
            var containerClient = await StartAzureConnection();
            var url = containerClient.Uri.AbsoluteUri;
            List<BlobObject> blobs = new List<BlobObject>();
            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
            {
                if (blobItem!=null)
                {
                    blobs.Add(new BlobObject { Url = url + "/" + blobItem.Name, Name = blobItem.Name });
                }
                
            }
            return View(blobs);
        }

        public async Task<IActionResult> Delete(string filename)
        {
            var containerClient = await StartAzureConnection();
            BlobClient blobClient = containerClient.GetBlobClient(filename);
            await blobClient.DeleteAsync();
            return  RedirectToAction("Files");
        }


        public async Task<IActionResult> Download(string filename)
        {
            return Redirect(filename);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
    public class BlobObject
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }
}
