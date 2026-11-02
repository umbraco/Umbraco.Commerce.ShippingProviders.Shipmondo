using System.Net;
using System.Text;
using FluentAssertions;
using Umbraco.Commerce.ShippingProviders.Shipmondo.Api;

namespace Umbraco.Commerce.ShippingProviders.Shipmondo.UnitTests
{
    public class ShipmondoClientTests
    {
        [Fact]
        public async Task GetQuoteListAsync_Should_Post_To_Quotes_List_On_The_Public_Api_Base_Address()
        {
            // preparation
            var handler = new StubHandler(_ => Json("[]"));
            var client = ShipmondoClient.Create(new StubHttpClientFactory(handler), CreateSettings());

            // execution
            await client.GetQuoteListAsync(new ShipmondoQuoteListRequest());

            // asserts
            handler.Requests.Should().ContainSingle();
            handler.Requests[0].Method.Should().Be(HttpMethod.Post);
            handler.Requests[0].Uri.Should().Be(new Uri("https://app.shipmondo.com/api/public/v3/quotes/list"));
        }

        [Fact]
        public async Task GetQuoteListAsync_Should_Send_Basic_Auth_Built_From_ApiUser_And_ApiKey()
        {
            // preparation
            var handler = new StubHandler(_ => Json("[]"));
            var client = ShipmondoClient.Create(new StubHttpClientFactory(handler), CreateSettings("my-user", "my-key"));

            // execution
            await client.GetQuoteListAsync(new ShipmondoQuoteListRequest());

            // asserts
            var auth = handler.Requests[0].Authorization!;
            auth.Scheme.Should().Be("Basic");
            Encoding.UTF8.GetString(Convert.FromBase64String(auth.Parameter!)).Should().Be("my-user:my-key");
        }

        [Fact]
        public async Task GetQuoteListAsync_Should_Accept_Json()
        {
            // preparation
            var handler = new StubHandler(_ => Json("[]"));
            var client = ShipmondoClient.Create(new StubHttpClientFactory(handler), CreateSettings());

            // execution
            await client.GetQuoteListAsync(new ShipmondoQuoteListRequest());

            // asserts
            handler.Requests[0].Accept.Should().Equal("application/json");
        }

        [Fact]
        public async Task GetQuoteListAsync_Should_Send_The_Request_As_Json_Body()
        {
            // preparation
            var handler = new StubHandler(_ => Json("[]"));
            var client = ShipmondoClient.Create(new StubHttpClientFactory(handler), CreateSettings());
            var request = new ShipmondoQuoteListRequest();
            request.Receiver.CountryCode = "SE";

            // execution
            await client.GetQuoteListAsync(request);

            // asserts
            handler.Requests[0].Body.Should().Contain("\"country_code\":\"SE\"");
            handler.Requests[0].ContentType.Should().Be("application/json");
        }

        [Fact]
        public async Task GetQuoteListAsync_Should_Return_Deserialized_Quotes()
        {
            // preparation
            var handler = new StubHandler(_ => Json("""[{"carrier_code":"gls","product_code":"business","price":10,"price_before_vat":8,"currency_code":"DKK"}]"""));
            var client = ShipmondoClient.Create(new StubHttpClientFactory(handler), CreateSettings());

            // execution
            var quotes = (await client.GetQuoteListAsync(new ShipmondoQuoteListRequest())).ToList();

            // asserts
            quotes.Should().ContainSingle().Which.CarrierCode.Should().Be("gls");
        }

        [Theory]
        [InlineData(null, "key")]
        [InlineData("", "key")]
        [InlineData("   ", "key")]
        [InlineData("user", null)]
        [InlineData("user", "")]
        [InlineData("user", " ")]
        public void Create_Should_Throw_When_Credentials_Are_Missing(string? user, string? key)
        {
            // preparation
            var factory = new StubHttpClientFactory(new StubHandler(_ => Json("[]")));

            // execution
            var act = () => ShipmondoClient.Create(factory, CreateSettings(user, key));

            // asserts
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_Should_Throw_When_Settings_Are_Null()
        {
            // preparation
            var factory = new StubHttpClientFactory(new StubHandler(_ => Json("[]")));

            // execution
            var act = () => ShipmondoClient.Create(factory, null!);

            // asserts
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task GetPaginatedFromJsonAsync_Should_Read_Pagination_Headers_And_Items()
        {
            // preparation
            var handler = new StubHandler(_ =>
            {
                var resp = Json("""[{"code":"gls"},{"code":"dao"}]""");
                resp.Headers.Add("X-Current-Page", "2");
                resp.Headers.Add("X-Per-Page", "50");
                resp.Headers.Add("X-Total-Count", "120");
                resp.Headers.Add("X-Total-Pages", "3");
                return resp;
            });
            using var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };

            // execution
            var result = await http.GetPaginatedFromJsonAsync<Dictionary<string, string>>("carriers");

            // asserts
            result.CurrentPage.Should().Be(2);
            result.ItemsPerPage.Should().Be(50);
            result.TotalItems.Should().Be(120);
            result.TotalPages.Should().Be(3);
            result.Items.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetPaginatedFromJsonAsync_Should_Default_Pagination_Values_To_Zero_When_Headers_Are_Absent()
        {
            // preparation
            var handler = new StubHandler(_ => Json("[]"));
            using var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };

            // execution
            var result = await http.GetPaginatedFromJsonAsync<Dictionary<string, string>>("carriers");

            // asserts
            result.CurrentPage.Should().Be(0);
            result.ItemsPerPage.Should().Be(0);
            result.TotalItems.Should().Be(0);
            result.TotalPages.Should().Be(0);
        }

        [Fact]
        public void AppendQueryString_Should_Start_A_Query_String_And_Skip_Blank_Values()
        {
            // preparation
            var parameters = new[]
            {
                new KeyValuePair<string, string>("a", "1"),
                new KeyValuePair<string, string>("b", ""),
                new KeyValuePair<string, string>("c", "3"),
            };

            // execution
            var result = "carriers".AppendQueryString(parameters);

            // asserts
            result.Should().Be("carriers?a=1&c=3");
        }

        [Fact]
        public void AppendQueryString_Should_Append_To_An_Existing_Query_String()
        {
            // preparation
            var parameters = new[] { new KeyValuePair<string, string>("b", "2") };

            // execution
            var result = "carriers?a=1".AppendQueryString(parameters);

            // asserts
            result.Should().Be("carriers?a=1&b=2");
        }

        [Fact]
        public void AppendQueryString_Should_Return_Endpoint_Unchanged_When_There_Are_No_Values()
        {
            // execution
            var result = "carriers".AppendQueryString([]);

            // asserts
            result.Should().Be("carriers");
        }

        private static ShipmondoSettings CreateSettings(string? user = "user", string? key = "key")
            => new() { ApiUser = user!, ApiKey = key! };

        private static HttpResponseMessage Json(string body)
            => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

        private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
        }

        private sealed record CapturedRequest(
            HttpMethod Method,
            Uri? Uri,
            System.Net.Http.Headers.AuthenticationHeaderValue? Authorization,
            string[] Accept,
            string Body,
            string? ContentType);

        private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
        {
            public List<CapturedRequest> Requests { get; } = [];

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                string body = request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);

                Requests.Add(new CapturedRequest(
                    request.Method,
                    request.RequestUri,
                    request.Headers.Authorization,
                    request.Headers.Accept.Select(x => x.MediaType!).ToArray(),
                    body,
                    request.Content?.Headers.ContentType?.MediaType));

                return respond(request);
            }
        }
    }
}
