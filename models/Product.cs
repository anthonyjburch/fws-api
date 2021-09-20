using System.Collections.Generic;

namespace fws.api.models
{
    public class Product
    {
        public string Handle { get; set; }
        public string Vendor { get; set; }
        public string Title { get; set; }
        public IEnumerable<Variant> Variants { get; set; }
    }
}
