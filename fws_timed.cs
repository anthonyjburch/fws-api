using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fws.api.models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestSharp;
using Microsoft.Extensions.Configuration;

namespace fws.api
{
    public static class fws_timed
    {
        [FunctionName("fws_timed")]
        public static async Task Run([TimerTrigger("0 0 */1 * * *")] TimerInfo myTimer, ILogger log, ExecutionContext context)
        {
            var client = new RestClient("https://fatwreck.com");
            var page = 1;
            var results = 1;
            var products = new List<Product>();
            var items = new List<Item>();

            while (results != 0)
            {
                var request = new RestRequest($"products.json?page={page}");
                var response = await client.GetAsync<Container>(request);

                products.AddRange(response.Products);

                results = response.Products.Count();
                page++;
            }

            var config = new ConfigurationBuilder()
                                .SetBasePath(context.FunctionAppDirectory)
                                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                                .AddEnvironmentVariables()
                                .Build();
            var connString = config.GetConnectionString("StorageAccount");
            var storageAccount = CloudStorageAccount.Parse(connString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference("$web/assets");
            var stockBlob = blobContainer.GetBlockBlobReference("stock.json");

            var existingStockJson = await stockBlob.DownloadTextAsync();
            var existingStock = JsonConvert.DeserializeObject<IEnumerable<Item>>(existingStockJson);

            foreach (var product in products)
            {
                foreach (var variant in product.Variants)
                {
                    var existingItem = existingStock.FirstOrDefault(i => i.Id == variant.Id);
                    items.Add(new Item
                    {
                        Id = variant.Id,
                        Handle = product.Handle,
                        Artist = product.Vendor,
                        Title = product.Title,
                        Description = variant.Title,
                        ProductType = product.ProductType,
                        Format = GetFormat(product.ProductType, variant.Title),
                        Available = variant.Available,
                        DateUpdated = (existingItem == null || (!existingItem.Available && variant.Available)) ? DateTime.UtcNow : existingItem.DateUpdated
                    });
                }
            }

            var jsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            var stockJson = JsonConvert.SerializeObject(items, jsonSerializerSettings);


            stockBlob.Properties.ContentType = "application/json";
            stockBlob.Properties.CacheControl = "max-age=1";
            using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(stockJson)))
            {
                await stockBlob.UploadFromStreamAsync(stream);
            }

            var settings = new List<Setting>
            {
                new Setting
                {
                    Key = "LastUpdated",
                    Value = DateTime.UtcNow
                }
            };

            var settingsJson = JsonConvert.SerializeObject(settings, jsonSerializerSettings);

            var settingsBlob = blobContainer.GetBlockBlobReference("settings.json");
            settingsBlob.Properties.ContentType = "application/json";
            settingsBlob.Properties.CacheControl = "max-age=1";
            using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(settingsJson)))
            {
                await settingsBlob.UploadFromStreamAsync(stream);
            }


            log.LogInformation($"Wrote {items.Count()} items to storage.");
        }

        private static string GetFormat(ProductType type, string desc)
        {
            desc = desc.ToLower();

            if (desc.Contains("vinyl")
                || desc.Contains("lp")
                || desc.Contains("12\"")
                || desc.Contains("12 inch")
                || desc.Contains("12inch")
                || desc.Contains("10\"")
                || desc.Contains("10 inch")
                || desc.Contains("10inch")
                || desc.Contains("7\"")
                || desc.Contains("7 inch")
                || desc.Contains("7inch")
                )
            {
                return "vinyl";
            }

            if (desc.Contains("cassette"))
            {
                return "cassette";
            }

            if (desc.Contains("cd"))
            {
                return "cd";
            }

            if (desc.Contains("dvd"))
            {
                return "dvd";
            }

            if (desc.Equals("digital"))
            {
                return "digital";
            }

            if (type == ProductType.Merch)
            {
                return "merch";
            }

            return "unknown";
        }
    }
}
