using Umbraco.Commerce.Core.ShippingProviders;

namespace Umbraco.Commerce.ShippingProviders.Shipmondo
{
    public class ShipmondoSettings
    {
        [ShippingProviderSetting(SortOrder = 100)]
        public string ApiUser { get; set; }

        [ShippingProviderSetting(SortOrder = 200)]
        public string ApiKey { get; set; }

        [ShippingProviderSetting(SortOrder = 10000)]
        public bool TestMode { get; set; }
    }
}
