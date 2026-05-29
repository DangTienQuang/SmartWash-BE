using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs
{
    public class CreateBusinessProfileDTO
    {
        public string CompanyName { get; set; } = null!;

        public string? TaxCode { get; set; }

        public string? BusinessAddress { get; set; }

        public string? BillingEmail { get; set; }

        public string? RepresentativeName { get; set; }

        public int? PaymentTermDays { get; set; }
    }

    public class BusinessProfileResponseDTO
    {
        public int BusinessProfileId { get; set; }

        public string CompanyName { get; set; } = null!;

        public string? TaxCode { get; set; }
        public string? BusinessAddress { get; set; }
    }
}
