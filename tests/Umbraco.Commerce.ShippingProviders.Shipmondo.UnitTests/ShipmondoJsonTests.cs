using System.Text.Json;
using FluentAssertions;
using Umbraco.Commerce.ShippingProviders.Shipmondo.Api;

namespace Umbraco.Commerce.ShippingProviders.Shipmondo.UnitTests
{
    public class ShipmondoJsonTests
    {
        [Fact]
        public void QuoteListRequest_Should_Serialize_With_Shipmondo_Property_Names()
        {
            // preparation
            var request = new ShipmondoQuoteListRequest
            {
                Sender = new ShipmondoAddress { Address1 = "a1", Address2 = "a2", City = "c", ZipCode = "z", CountryCode = "DK" },
                Receiver = new ShipmondoAddress { CountryCode = "SE" }
            };
            request.Parcels.Add(new ShipmondoParcel
            {
                Description = "ORD-1",
                Weight = 1500,
                Length = 10,
                Width = 20,
                Height = 30,
                DeclaredValue = new ShimpondoPrice { Amount = 12.5m, CurrencyCode = "DKK" }
            });

            // execution
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(request));

            // asserts
            var root = doc.RootElement;
            root.EnumerateObject().Select(x => x.Name).Should().BeEquivalentTo("sender", "receiver", "parcels");

            var sender = root.GetProperty("sender");
            sender.GetProperty("address1").GetString().Should().Be("a1");
            sender.GetProperty("address2").GetString().Should().Be("a2");
            sender.GetProperty("city").GetString().Should().Be("c");
            sender.GetProperty("zipcode").GetString().Should().Be("z");
            sender.GetProperty("country_code").GetString().Should().Be("DK");

            var parcel = root.GetProperty("parcels")[0];
            parcel.GetProperty("description").GetString().Should().Be("ORD-1");
            parcel.GetProperty("weight").GetInt32().Should().Be(1500);
            parcel.GetProperty("length").GetInt32().Should().Be(10);
            parcel.GetProperty("width").GetInt32().Should().Be(20);
            parcel.GetProperty("height").GetInt32().Should().Be(30);
            parcel.TryGetProperty("quantity", out _).Should().BeTrue();
            parcel.GetProperty("declared_value").GetProperty("amount").GetDecimal().Should().Be(12.5m);
            parcel.GetProperty("declared_value").GetProperty("currency_code").GetString().Should().Be("DKK");
        }

        [Fact]
        public void QuoteList_Should_Deserialize_A_Shipmondo_Response()
        {
            // preparation
            const string json = """
                [
                  {
                    "carrier_code": "gls",
                    "product_code": "business",
                    "service_codes": "EMAIL_NT,SMS_NT",
                    "description": "GLS Business Parcel",
                    "price": 62.5,
                    "price_before_vat": 50.0,
                    "currency_code": "DKK",
                    "some_future_field": { "a": 1 }
                  },
                  {
                    "carrier_code": "dao",
                    "product_code": "pakke",
                    "service_codes": null,
                    "description": "DAO Pakke",
                    "price": 49,
                    "price_before_vat": 39.2,
                    "currency_code": "DKK"
                  }
                ]
                """;

            // execution
            var quotes = JsonSerializer.Deserialize<List<ShipmondoQuote>>(json)!;

            // asserts
            quotes.Should().HaveCount(2);
            quotes[0].CarrierCode.Should().Be("gls");
            quotes[0].ProductCode.Should().Be("business");
            quotes[0].ServiceCodes.Should().Be("EMAIL_NT,SMS_NT");
            quotes[0].Description.Should().Be("GLS Business Parcel");
            quotes[0].Price.Should().Be(62.5m);
            quotes[0].PriceBeforeVat.Should().Be(50m);
            quotes[0].CurrencyCode.Should().Be("DKK");
            quotes[1].ServiceCodes.Should().BeNull();
            quotes[1].PriceBeforeVat.Should().Be(39.2m);
        }
    }
}
