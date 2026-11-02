using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Commerce.Extensions;
using Umbraco.Commerce.ShippingProviders.Shipmondo.Api.Models;

namespace Umbraco.Commerce.ShippingProviders.Shipmondo.Api
{
    public class ShipmondoClient
    {
        // https://shipmondo.dev/docs/sandbox
        private const string SandboxBaseAddress = "https://sandbox.shipmondo.com/api/public/v3/";
        private const string LiveBaseAddress = "https://app.shipmondo.com/api/public/v3/";

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
            _httpClient.BaseAddress = new Uri(settings.TestMode ? SandboxBaseAddress : LiveBaseAddress);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", $"{settings.ApiUser}:{settings.ApiKey}".Base64Encode());

            _settings = settings;
        }

        /// <summary>
        /// Gets quotes for the request.
        /// </summary>
        /// <exception cref="HttpRequestException">Shipmondo returned an error status, or a body that isn't a quote list.</exception>
        public async Task<IEnumerable<ShipmondoQuote>> GetQuoteListAsync(ShipmondoQuoteListRequest req, CancellationToken cancellationToken = default)
        {
            var result = await GetQuoteListResultAsync(req, cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                throw new HttpRequestException(
                    $"Shipmondo quote request failed with HTTP {(int)result.StatusCode}: {result.ErrorDetail}",
                    null,
                    result.StatusCode);
            }

            return result.Quotes;
        }

        /// <summary>
        /// Gets quotes for the request without throwing on an error status or an unreadable body.
        /// </summary>
        internal async Task<ShipmondoQuoteListResult> GetQuoteListResultAsync(ShipmondoQuoteListRequest req, CancellationToken cancellationToken = default)
        {
            using (var resp = await _httpClient.PostAsJsonAsync("quotes/list", req, cancellationToken).ConfigureAwait(false))
            {
                var body = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                return ShipmondoQuoteListResult.FromResponse(resp.StatusCode, body);
            }
        }

    }

    internal sealed class ShipmondoQuoteListResult
    {
        // The same defaults ReadFromJsonAsync uses, so quotes deserialize exactly as before
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        private const int MaxErrorDetailLength = 1000;

        private ShipmondoQuoteListResult(HttpStatusCode statusCode, IReadOnlyList<ShipmondoQuote>? quotes, string errorDetail)
        {
            StatusCode = statusCode;
            Quotes = quotes ?? Array.Empty<ShipmondoQuote>();
            IsSuccess = quotes != null;
            ErrorDetail = errorDetail;
        }

        public bool IsSuccess { get; }

        public HttpStatusCode StatusCode { get; }

        public IReadOnlyList<ShipmondoQuote> Quotes { get; }

        /// <summary>The response body, truncated, when the call failed. Never contains the request's credentials.</summary>
        public string ErrorDetail { get; }

        public static ShipmondoQuoteListResult FromResponse(HttpStatusCode statusCode, string? body)
        {
            var isSuccess = (int)statusCode >= 200 && (int)statusCode <= 299;

            if (isSuccess && !string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    var quotes = JsonSerializer.Deserialize<List<ShipmondoQuote>>(body, JsonOptions);
                    if (quotes != null)
                    {
                        return new ShipmondoQuoteListResult(statusCode, quotes.Where(x => x != null).ToList(), string.Empty);
                    }
                }
                catch (JsonException)
                {
                    // Not a quote list; reported as a failure below
                }
            }

            return new ShipmondoQuoteListResult(statusCode, null, Truncate(body));
        }

        private static string Truncate(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Length > MaxErrorDetailLength
                ? string.Concat(value.AsSpan(0, MaxErrorDetailLength), "...")
                : value;
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
