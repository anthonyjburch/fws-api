using System.Collections.Generic;
using Newtonsoft.Json;

namespace fws.api.models
{
    public class Product
    {
        public long Id { get; set; }

        public string Title { get; set; }

        public string Handle { get; set; }

        public string Vendor { get; set; }

        [JsonProperty("product_type")]
        public ProductType ProductType { get; set; }

        public IEnumerable<Variant> Variants { get; set;}
    }
}