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
                    var item = new Item
                    {
                        Id = variant.Id,
                        Handle = product.Handle,
                        Artist = product.Vendor,
                        Title = product.Title,
                        Description = variant.Title,
                        ProductType = product.ProductType,
                        Format = GetFormat(product.ProductType, product.Title, variant.Title),
                        Available = variant.Available,
                        DateUpdated = (existingItem == null || (!existingItem.Available && variant.Available)) ? DateTime.UtcNow : existingItem.DateUpdated
                    };

                    if (item.Available && item.Format == "vinyl")
                    {
                        try
                        {
                            var cart = await client.PostAsync<Cart>(new RestRequest("cart/clear.js"));
                            cart.items.Add(new CartItem { id = item.Id, quantity = 1});
                            cart = await client.PostAsync<Cart>(new RestRequest("cart/add.js").AddJsonBody(cart));
                            cart = await client.PostAsync<Cart>(new RestRequest("cart/change.js").AddJsonBody(new {
                                id = cart.items[0].key,
                                quantity = 9999
                            }));

                            item.Quantity = cart.items[0].quantity;
                        }

                        catch (Exception ex)
                        {
                            log.LogError(ex, "Error getting quantity", item);
                        }
                    }
                    
                    items.Add(item);
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


            log.LogInformation($"Found {items.Count()} items.");
        }

        private static string GetFormat(ProductType type, string productTitle, string variantTitle)
        {
            variantTitle = variantTitle.ToLower();
            productTitle = productTitle.ToLower();

            var vinylDescriptors = new string[] { "vinyl", "lp", "12\"", "12 inch", "12inch", "10\"", "10 inch", "10inch", "7\"", "7 inch", "7inch"  };

            if (vinylDescriptors.Any(d => variantTitle.Contains(d))) return "vinyl";
            if (vinylDescriptors.Any(d => productTitle.Contains(d))) return "vinyl";
            if (variantTitle.Contains("cassette")) return "cassette";
            if (variantTitle.Contains("cd")) return "cd";
            if (variantTitle.Contains("dvd")) return "dvd";
            if (variantTitle.Contains("digital")) return "digital";
            if (type == ProductType.Merch) return "merch";

            return "unknown";
        }
    }
}
