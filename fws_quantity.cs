using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using fws.api.models;

namespace fws.api
{
    public static class fws_quantity
    {
        [FunctionName("fws_quantity")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string strId = req.Query["id"];
            
            if (!long.TryParse(strId, out long lngId))
            {
                return new BadRequestObjectResult(new { Error = "Invalid ID"});
            }

            var client = new RestClient("https://fatwreck.com");
            var cart = await client.PostAsync<Cart>(new RestRequest("cart/clear.js"));
            cart.items.Add(new CartItem { id = lngId, quantity = 1 });
            cart = await client.PostAsync<Cart>(new RestRequest("cart/add.js").AddJsonBody(cart));
            cart = await client.PostAsync<Cart>(new RestRequest("cart/change.js").AddJsonBody(new
            {
                id = cart.items[0].key,
                quantity = 999999
            }));

            return new OkObjectResult(new {
                id = cart.items[0].id,
                artist = cart.items[0].vendor,
                title = cart.items[0].title,
                quantity = cart.items[0].quantity
            });
        }
    }
}
