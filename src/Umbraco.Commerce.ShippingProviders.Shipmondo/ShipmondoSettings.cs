using Umbraco.Commerce.Core.ShippingProviders;

namespace Umbraco.Commerce.ShippingProviders.Shipmondo
{
    public class ShipmondoSettings
    {
        [ShippingProviderSetting(Name = "API User",
            Description = "The API User ID from the Shipmondo portals 'Settings > API Access' area.",
            SortOrder = 100)]
        public string ApiUser { get; set; }

        [ShippingProviderSetting(Name = "API Key",
            Description = "The API User ID from the Shipmondo portals 'Settings > API Access' area.",
            SortOrder = 200)]
        public string ApiKey { get; set; }

        [ShippingProviderSetting(Name = "Test Mode",
            Description = "Set whether to run in test mode.",
            SortOrder = 10000)]
        public bool TestMode { get; set; }
    }
}
