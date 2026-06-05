using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs.Business
{
    public class CreateInvoiceDTO
    {
        public int BookingId { get; set; }

        public decimal TaxAmount { get; set; }
    }

    public class InvoiceResponseDTO
    {
        public int InvoiceId { get; set; }

        public string InvoiceCode { get; set; } = null!;

        public decimal TotalAmount { get; set; }

        public DateTime IssuedAt { get; set; }
    }
}
