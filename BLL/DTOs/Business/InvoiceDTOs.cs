using BLL.DTOs.Fleet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs.Business
{
    public class InvoiceDTO
    {
        public int InvoiceId { get; set; }
        public string InvoiceCode { get; set; } = null!;
        public string InvoiceType { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime IssuedAt { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public List<InvoiceItemDTO> Items { get; set; } = new();
    }

    public class InvoiceItemDTO
    {
        public int InvoiceItemId { get; set; }
        public string Description { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
    }

    public class InvoiceListDTO
    {
        public int InvoiceId { get; set; }
        public string InvoiceCode { get; set; } = null!;
        public DateTime IssuedAt { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = null!;
        public string LicensePlate { get; set; } = null!;
    }

    public class InvoiceDetailDTO
    {
        public int InvoiceId { get; set; }
        public string InvoiceCode { get; set; } = null!;
        public DateTime IssuedAt { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = null!;
        public string LicensePlate { get; set; } = null!;
        public List<InvoiceItemDTO> Items { get; set; } = new();
    }

    public class MonthlyStatementDTO
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int TotalWashes { get; set; }
        public decimal TotalCost { get; set; }
        public List<VehicleStatementDTO> Vehicles { get; set; } = new();
    }
}

