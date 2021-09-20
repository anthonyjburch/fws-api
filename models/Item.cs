using System;

namespace fws.api.models
{
    public class Item
    {
        public long Id { get; set; }
        public string Handle { get; set; }
        public string Artist { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Format { get; set; }
        public bool Available { get; set; }
        public DateTime DateUpdated { get; set; }
    }
}
