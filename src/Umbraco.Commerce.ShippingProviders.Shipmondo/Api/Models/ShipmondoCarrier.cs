using System.Text.Json.Serialization;

namespace Umbraco.Commerce.ShippingProviders.Shipmondo.Api.Models
{
    public class ShipmondoCarrier
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
