using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Commerce.Extensions;
using Umbraco.Commerce.ShippingProviders.Shipmondo.Api.Models;

namespace Umbraco.Commerce.ShippingProviders.Shipmondo.Api
{
    public class ShipmondoClient
    {
        private readonly HttpClient _httpClient;
        private readonly ShipmondoSettings _settings;

        public static ShipmondoClient Create(IHttpClientFactory httpClientFactory, ShipmondoSettings settings)
            => new ShipmondoClient(httpClientFactory.CreateClient(), settings);

        private ShipmondoClient(HttpClient httpClient, ShipmondoSettings settings)
        {
            settings.MustNotBeNull(nameof(settings));
            settings.ApiUser.MustNotBeNullOrWhiteSpace(nameof(settings.ApiUser));
            settings.ApiKey.MustNotBeNullOrWhiteSpace(nameof(settings.ApiKey));

            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://app.shipmondo.com/api/public/v3/");
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", $"{settings.ApiUser}:{settings.ApiKey}".Base64Encode());

            _settings = settings;
        }

        public async Task<IEnumerable<ShipmondoQuote>> GetQuoteListAsync(ShipmondoQuoteListRequest req, CancellationToken cancellationToken = default)
        {
            using (var resp = await _httpClient.PostAsJsonAsync("quotes/list", req, cancellationToken).ConfigureAwait(false))
            {
                return await resp.Content.ReadFromJsonAsync<IEnumerable<ShipmondoQuote>>(cancellationToken).ConfigureAwait(false);
            }
        }

    }

    public class ShipmondoQuote
    {
        [JsonPropertyName("carrier_code")]
        public string CarrierCode { get; set; }

        [JsonPropertyName("product_code")]
        public string ProductCode { get; set; }

        [JsonPropertyName("service_codes")]
        public string ServiceCodes { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("price_before_vat")]
        public decimal PriceBeforeVat { get; set; }

        [JsonPropertyName("currency_code")]
        public string CurrencyCode { get; set; }
    }

    public class ShipmondoQuoteListRequest
    {
        [JsonPropertyName("sender")]
        public ShipmondoAddress Sender { get; set; }

        [JsonPropertyName("receiver")]
        public ShipmondoAddress Receiver { get; set; }

        [JsonPropertyName("parcels")]
        public List<ShipmondoParcel> Parcels { get; set; }

        public ShipmondoQuoteListRequest()
        {
            Sender = new ShipmondoAddress();
            Receiver = new ShipmondoAddress();
            Parcels = new List<ShipmondoParcel>();
        }
    }

    public class ShipmondoAddress
    {
        [JsonPropertyName("address1")]
        public string Address1 { get; set; }

        [JsonPropertyName("address2")]
        public string Address2 { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }

        [JsonPropertyName("zipcode")]
        public string ZipCode { get; set; }

        [JsonPropertyName("country_code")]
        public string CountryCode { get; set; }
    }

    public class ShipmondoParcel
    {
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("quantity")]
        public int? Quantity { get; set; }

        [JsonPropertyName("weight")]
        public int Weight { get; set; }

        [JsonPropertyName("length")]
        public int? Length { get; set; }

        [JsonPropertyName("width")]
        public int? Width { get; set; }

        [JsonPropertyName("height")]
        public int? Height { get; set; }

        [JsonPropertyName("volume")]
        public int? Volume { get; set; }

        // running_metre
        // packaging
        // dangerous_goods

        [JsonPropertyName("declared_value")]
        public ShimpondoPrice DeclaredValue { get; set; }
    }

    public class ShimpondoPrice
    {

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("currency_code")]
        public string CurrencyCode { get; set; }
    }

    internal static class HttpClientExtensions
    {
        public static async Task<ShipmondoPaginatedResult<T>> GetPaginatedFromJsonAsync<T>(this HttpClient client, string requestUri)
        {
            using HttpResponseMessage resp = await client.GetAsync(requestUri).ConfigureAwait(false);

            return await ParsePaginatedResultAsync<T>(resp).ConfigureAwait(true);
        }

        private static async Task<ShipmondoPaginatedResult<T>> ParsePaginatedResultAsync<T>(HttpResponseMessage msg)
        {
            var result = new ShipmondoPaginatedResult<T>();

            if (msg.Headers.TryGetValues("X-Current-Page", out var v1))
            {
                result.CurrentPage = int.Parse($"0{v1.FirstOrDefault()}");
            }

            if (msg.Headers.TryGetValues("X-Per-Page", out var v2))
            {
                result.ItemsPerPage = int.Parse($"0{v2.FirstOrDefault()}");
            }

            if (msg.Headers.TryGetValues("X-Total-Count", out var v3))
            {
                result.TotalItems = int.Parse($"0{v3.FirstOrDefault()}");
            }

            if (msg.Headers.TryGetValues("X-Total-Pages", out var v4))
            {
                result.TotalPages = int.Parse($"0{v4.FirstOrDefault()}");
            }

            result.Items = await msg.Content.ReadFromJsonAsync<IEnumerable<T>>().ConfigureAwait(true);

            return result;
        }
    }

    internal static class StringExtensions
    {
        public static string AppendQueryString(this string endpoint, IEnumerable<KeyValuePair<string, string>> parameters)
        {
            var e = endpoint;
            var hasQuery = e.IndexOf('?') > -1;

            foreach (var kvp in parameters)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Value))
                {
                    e += (!hasQuery ? "?" : "&") + kvp.Key + "=" + kvp.Value;
                    hasQuery = true;
                }
            }

            return e;
        }
    }
}
