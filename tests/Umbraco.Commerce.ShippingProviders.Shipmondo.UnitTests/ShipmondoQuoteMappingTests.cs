using FluentAssertions;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.ShippingProviders.Shipmondo.Api;

namespace Umbraco.Commerce.ShippingProviders.Shipmondo.UnitTests
{
    public class ShipmondoQuoteMappingTests
    {
        private static readonly Guid CurrencyId = Guid.NewGuid();
        private static readonly Guid PackageId = Guid.NewGuid();

        [Fact]
        public void CreateShippingRates_Should_Only_Return_Quotes_In_The_Order_Currency_Ignoring_Case()
        {
            // preparation
            var quotes = new[]
            {
                CreateQuote("gls", "business", "DKK"),
                CreateQuote("dao", "pakke", "dkk"),
                CreateQuote("postnord", "mypack", "SEK"),
            };

            // execution
            var rates = ShipmondoShippingProvider.CreateShippingRates(quotes, "DKK", CurrencyId, PackageId);

            // asserts
            rates.Select(x => x.Option.Id).Should().Equal("gls__business", "dao__pakke");
        }

        [Fact]
        public void CreateShippingRates_Should_Split_Price_Into_Net_And_Vat()
        {
            // preparation
            var quotes = new[] { CreateQuote("gls", "business", "DKK", price: 125m, priceBeforeVat: 100m) };

            // execution
            var rate = ShipmondoShippingProvider.CreateShippingRates(quotes, "DKK", CurrencyId, PackageId).Single();

            // asserts
            rate.Value.WithoutTax.Should().Be(100m);
            rate.Value.Tax.Should().Be(25m);
            rate.Value.WithTax.Should().Be(125m);
            rate.Value.CurrencyId.Should().Be(CurrencyId);
        }

        [Fact]
        public void CreateShippingRates_Should_Use_Quote_Description_As_Option_Name()
        {
            // preparation
            var quotes = new[] { CreateQuote("gls", "business", "DKK", description: "GLS Business Parcel") };

            // execution
            var rate = ShipmondoShippingProvider.CreateShippingRates(quotes, "DKK", CurrencyId, PackageId).Single();

            // asserts
            rate.Option.Name.Should().Be("GLS Business Parcel");
        }

        [Fact]
        public void CreateShippingRates_Should_Set_Package_Id()
        {
            // preparation
            var quotes = new[] { CreateQuote("gls", "business", "DKK") };

            // execution
            var rate = ShipmondoShippingProvider.CreateShippingRates(quotes, "DKK", CurrencyId, PackageId).Single();

            // asserts
            rate.PackageId.Should().Be(PackageId);
        }

        [Theory]
        [InlineData("gls", "business", "gls__business")]
        [InlineData("gls", "", "gls")]
        [InlineData("", "business", "business")]
        [InlineData("", "", "")]
        public void CreateShippingRates_Should_Build_Composite_Id_Trimmed_Of_Underscores(string carrier, string product, string expected)
        {
            // preparation
            var quotes = new[] { CreateQuote(carrier, product, "DKK") };

            // execution
            var rate = ShipmondoShippingProvider.CreateShippingRates(quotes, "DKK", CurrencyId, PackageId).Single();

            // asserts
            rate.Option.Id.Should().Be(expected);
        }

        [Fact]
        public void CreateShippingRates_Should_Return_Empty_When_No_Quote_Matches_Currency()
        {
            // preparation
            var quotes = new[] { CreateQuote("gls", "business", "SEK") };

            // execution
            var rates = ShipmondoShippingProvider.CreateShippingRates(quotes, "DKK", CurrencyId, PackageId);

            // asserts
            rates.Should().BeEmpty();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void CreateShippingRates_Should_Skip_Quotes_Without_A_Currency(string? currency)
        {
            // preparation
            var quotes = new[]
            {
                CreateQuote("gls", "business", currency!),
                CreateQuote("dao", "pakke", "DKK"),
            };

            // execution
            var rates = ShipmondoShippingProvider.CreateShippingRates(quotes, "DKK", CurrencyId, PackageId);

            // asserts
            rates.Select(x => x.Option.Id).Should().Equal("dao__pakke");
        }

        private static ShipmondoQuote CreateQuote(
            string carrier,
            string product,
            string currency,
            decimal price = 10m,
            decimal priceBeforeVat = 8m,
            string description = "Description")
            => new()
            {
                CarrierCode = carrier,
                ProductCode = product,
                CurrencyCode = currency,
                Price = price,
                PriceBeforeVat = priceBeforeVat,
                Description = description
            };
    }
}
