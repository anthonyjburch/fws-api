using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fws.api.models
{
    public class Cart
    {
        public List<CartItem> items { get; set; } = new List<CartItem>();
    }

    public class CartItem
    {
        public long id { get; set; }
        public int quantity { get; set; }
        public string title { get; set; }
        public string vendor { get; set; }
        public string key { get; set; }
    }
}