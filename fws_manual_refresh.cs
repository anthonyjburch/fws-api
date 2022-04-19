using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RestSharp;
using System;

namespace fws.api
{
    public static class fws_manual_refresh
    {
        [FunctionName("fws_manual_refresh")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Triggering fws_timed");

            try
            {
                var master_key = System.Environment.GetEnvironmentVariable("FUNCTION_MASTER_KEY", EnvironmentVariableTarget.Process);

                var client = new RestClient("https://fatwreckstock.azurewebsites.net");
                
                var request = new RestRequest("admin/functions/fws_timed", Method.Post);
                request.AddHeader("x-functions-key", master_key);
                request.AddJsonBody(new {});
                
                var response = await client.ExecutePostAsync(request);
            }

            catch (Exception ex)
            {
                log.LogError(ex.Message, ex);

                return new BadRequestResult();
            }

            log.LogInformation("fws_timed triggered successfully");

            return new OkResult();
        }
    }
}
