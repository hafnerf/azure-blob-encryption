using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Extensions.Configuration;

namespace FHafner.EncryptBlob
{
    public static class HttpTriggerEncryptBlob
    {
        static IConfigurationRoot config;
        [FunctionName("HttpTriggerEncryptBlob")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log,
            Microsoft.Azure.WebJobs.ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            List<string> errors = new List<string>();

            config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            if (req.Form.Files.Count > 0)
            {
                foreach (var file in req.Form.Files)
                {
                    CloudBlockBlob blob = await CreateBlob(file.FileName, file.OpenReadStream());
                    if (!blob.Exists())
                    {
                        errors.Add(file.FileName);
                    }
                }
            }

            return errors.Count > 0
                ? new BadRequestObjectResult("Blob could not be created")
                : (ActionResult)new OkObjectResult("Blob created");
        }

        private async static Task<CloudBlockBlob> CreateBlob(string identifier, Stream stream)
        {
            AzureServiceTokenProvider tokenProvider = new AzureServiceTokenProvider();

            var keyVaultClient = new KeyVaultClient(
            new KeyVaultClient.AuthenticationCallback(tokenProvider.KeyVaultTokenCallback));
            KeyVaultKeyResolver cloudResolver = new KeyVaultKeyResolver(keyVaultClient);
            var rsa = cloudResolver.ResolveKeyAsync(
                        $"{config["keyvault"]}/keys/{config["key"]}",
                        CancellationToken.None).GetAwaiter().GetResult();

            string accessToken = await tokenProvider.GetAccessTokenAsync("https://storage.azure.com/");
            var tokenCredential = new TokenCredential(accessToken);
            var storageCredentials = new StorageCredentials(tokenCredential);
            CloudBlockBlob blob = new CloudBlockBlob(new Uri($"{config["storage_account"]}/{config["blob_container"]}/{identifier}"), storageCredentials);

            BlobEncryptionPolicy policy = new BlobEncryptionPolicy(rsa, null);
            BlobRequestOptions options = new BlobRequestOptions() { EncryptionPolicy = policy };

            await blob.UploadFromStreamAsync(stream, stream.Length, null, options, null);
            return blob;
        }
    }
}
