using System.Collections.Generic;

namespace Umbraco.Commerce.ShippingProviders.Shipmondo.Api.Models
{
    public class ShipmondoPaginatedResult<T>
    {
        public int CurrentPage { get; set; }

        public int ItemsPerPage { get; set; }

        public int TotalItems { get; set; }

        public int TotalPages { get; set; }

        public IEnumerable<T> Items { get; set; }
    }
}
