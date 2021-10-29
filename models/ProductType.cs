using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace fws.api.models
{
    [JsonConverter(typeof(ProductTypeEnumConverter))]
    public enum ProductType
    {
        Merch,
        Music,
        Other
    }

    public class ProductTypeEnumConverter : StringEnumConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            string val = reader.Value.ToString().ToLower();

            if (val != "merch" && val != "music") {
                return ProductType.Other;
            }

            return base.ReadJson(reader, objectType, existingValue, serializer);
        }
    }
}
