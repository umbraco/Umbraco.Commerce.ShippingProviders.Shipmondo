using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Commerce.Common.Logging;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.Core.ShippingProviders;
using Umbraco.Commerce.ShippingProviders.Shipmondo.Api;

namespace Umbraco.Commerce.ShippingProviders.Shipmondo
{
    [ShippingProvider("shipmondo")]
    public class ShipmondoShippingProvider(
        UmbracoCommerceContext ctx,
        IHttpClientFactory httpClientFactory,
        ILogger<ShipmondoShippingProvider> logger)
        : ShippingProviderBase<ShipmondoSettings>(ctx)
    {
        public override bool SupportsRealtimeRates => true;

        public override async Task<ShippingRatesResult> GetShippingRatesAsync(ShippingProviderContext<ShipmondoSettings> context, CancellationToken cancellationToken = default)
        {
            var package = context.Packages.FirstOrDefault();

            if (package == null || !package.HasMeasurements)
            {
                logger.Debug("Unable to calculate realtime Shipmondo rates as the package provided is invalid");
                return ShippingRatesResult.Empty;
            }

            var client = ShipmondoClient.Create(httpClientFactory, context.Settings);

            var request = CreateQuoteRequest(package, context.MeasurementSystem, context.Order.OrderNumber);

            var result = await client.GetQuoteListResultAsync(request, cancellationToken);

            if (!result.IsSuccess)
            {
                logger.Error("Failed to get Shipmondo realtime rates: HTTP {Status} {Detail}", (int)result.StatusCode, result.ErrorDetail);
                return ShippingRatesResult.Empty;
            }

            var orderCurrency = await Context.Services.CurrencyService.GetCurrencyAsync(context.Order.CurrencyId);

            return new ShippingRatesResult
            {
                Rates = CreateShippingRates(result.Quotes, orderCurrency.Code, context.Order.CurrencyId, package.Id)
            };
        }

        internal ShipmondoQuoteListRequest CreateQuoteRequest(Package package, MeasurementSystem measurementSystem, string orderNumber)
        {
            var request = new ShipmondoQuoteListRequest
            {
                Receiver = CreateAddress(package.ReceiverAddress),
                Sender = CreateAddress(package.SenderAddress)
            };

            var l = measurementSystem == MeasurementSystem.Metric ? package.Length : InToCm(package.Length);
            var w = measurementSystem == MeasurementSystem.Metric ? package.Width : InToCm(package.Width);
            var h = measurementSystem == MeasurementSystem.Metric ? package.Height : InToCm(package.Height);
            var wg = measurementSystem == MeasurementSystem.Metric ? package.Weight : LbToKg(package.Weight);

            request.Parcels.Add(new ShipmondoParcel
            {
                Description = orderNumber,
                Weight = (int)Math.Ceiling(wg * 1000), // Kg to Grams
                Length = (int)Math.Ceiling(l),
                Width = (int)Math.Ceiling(w),
                Height = (int)Math.Ceiling(h)
            });

            return request;
        }

        internal static List<ShippingRate> CreateShippingRates(IEnumerable<ShipmondoQuote> quotes, string currencyCode, Guid currencyId, Guid packageId)
            => quotes
                .Where(x => x.CurrencyCode.Equals(currencyCode, StringComparison.OrdinalIgnoreCase))
                .Select(x => new ShippingRate(
                    new Price(x.PriceBeforeVat, x.Price - x.PriceBeforeVat, currencyId),
                    new ShippingOption(CreateCompositeId(x.CarrierCode, x.ProductCode), x.Description),
                    packageId
                )).ToList();

        private static ShipmondoAddress CreateAddress(Address address)
            => new ShipmondoAddress
            {
                Address1 = address.AddressLine1,
                Address2 = address.AddressLine2,
                City = address.City,
                ZipCode = address.ZipCode,
                CountryCode = address.CountryIsoCode
            };

        private static string CreateCompositeId(string carrierCode, string productCode)
            => $"{carrierCode}__{productCode}".Trim('_');
    }
}
