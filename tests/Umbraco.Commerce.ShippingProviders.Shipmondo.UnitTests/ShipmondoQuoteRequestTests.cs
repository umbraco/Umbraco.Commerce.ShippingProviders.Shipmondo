using FluentAssertions;
using Umbraco.Commerce.Core.Models;

namespace Umbraco.Commerce.ShippingProviders.Shipmondo.UnitTests
{
    public class ShipmondoQuoteRequestTests
    {
        private readonly TestProvider _provider = new();

        [Fact]
        public void CreateQuoteRequest_Should_Map_Sender_And_Receiver_Addresses()
        {
            // preparation
            Package package = CreatePackage(10, 10, 10, 1);

            // execution
            var request = _provider.CreateQuoteRequest(package, MeasurementSystem.Metric, "ORD-1");

            // asserts
            request.Sender.Address1.Should().Be("Sender Street 1");
            request.Sender.Address2.Should().Be("Sender Floor 2");
            request.Sender.City.Should().Be("Odense");
            request.Sender.ZipCode.Should().Be("5000");
            request.Sender.CountryCode.Should().Be("DK");

            request.Receiver.Address1.Should().Be("Receiver Road 9");
            request.Receiver.Address2.Should().Be("Receiver Suite 4");
            request.Receiver.City.Should().Be("Stockholm");
            request.Receiver.ZipCode.Should().Be("11122");
            request.Receiver.CountryCode.Should().Be("SE");
        }

        [Fact]
        public void CreateQuoteRequest_Should_Create_Single_Parcel_Described_By_Order_Number()
        {
            // preparation
            Package package = CreatePackage(10, 10, 10, 1);

            // execution
            var request = _provider.CreateQuoteRequest(package, MeasurementSystem.Metric, "ORD-1");

            // asserts
            request.Parcels.Should().ContainSingle().Which.Description.Should().Be("ORD-1");
        }

        [Fact]
        public void CreateQuoteRequest_Should_Convert_Metric_Kilograms_To_Whole_Grams()
        {
            // preparation
            Package package = CreatePackage(10, 10, 10, 1.2345m);

            // execution
            var request = _provider.CreateQuoteRequest(package, MeasurementSystem.Metric, "ORD-1");

            // asserts
            request.Parcels[0].Weight.Should().Be(1235);
        }

        [Fact]
        public void CreateQuoteRequest_Should_Round_Metric_Dimensions_Up_To_Whole_Centimetres()
        {
            // preparation
            Package package = CreatePackage(10.1m, 20.01m, 30m, 1);

            // execution
            var request = _provider.CreateQuoteRequest(package, MeasurementSystem.Metric, "ORD-1");

            // asserts
            var parcel = request.Parcels[0];
            parcel.Length.Should().Be(11);
            parcel.Width.Should().Be(21);
            parcel.Height.Should().Be(30);
        }

        [Fact]
        public void CreateQuoteRequest_Should_Convert_Imperial_Dimensions_From_Inches_To_Whole_Centimetres()
        {
            // preparation (10in = 25.4cm, 4in = 10.16cm, 1in = 2.54cm)
            Package package = CreatePackage(10, 4, 1, 1);

            // execution
            var request = _provider.CreateQuoteRequest(package, MeasurementSystem.Imperial, "ORD-1");

            // asserts
            var parcel = request.Parcels[0];
            parcel.Length.Should().Be(26);
            parcel.Width.Should().Be(11);
            parcel.Height.Should().Be(3);
        }

        [Fact]
        public void CreateQuoteRequest_Should_Convert_Imperial_Pounds_To_Whole_Grams()
        {
            // preparation (2lb = 0.90718474kg)
            Package package = CreatePackage(10, 10, 10, 2);

            // execution
            var request = _provider.CreateQuoteRequest(package, MeasurementSystem.Imperial, "ORD-1");

            // asserts
            request.Parcels[0].Weight.Should().Be(908);
        }

        private static Package CreatePackage(decimal length, decimal width, decimal height, decimal weight)
            => new(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new Address("Sender Street 1", "Sender Floor 2", "Odense", null!, "DK", "5000"),
                new Address("Receiver Road 9", "Receiver Suite 4", "Stockholm", null!, "SE", "11122"),
                Guid.NewGuid(),
                [],
                length,
                width,
                height,
                weight);

        private sealed class TestProvider : ShipmondoShippingProvider
        {
            public TestProvider()
                : base(null!, null!, null!)
            {
            }
        }
    }
}
