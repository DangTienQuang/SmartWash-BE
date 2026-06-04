using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs
{
    public class BusinessBookingResponseDTO
    {
        public int BookingId { get; set; }

        public DateTime BookingDate { get; set; }

        public string BookingStatus { get; set; } = null!;

        public decimal TotalAmount { get; set; }

        public int TotalVehicles { get; set; }

        public List<BusinessBookingDetailDTO> Vehicles { get; set; } = new();
    }

    public class BusinessBookingDetailDTO
    {
        public int DetailId { get; set; }

        public string LicensePlate { get; set; } = null!;

        public string ServiceName { get; set; } = null!;

        public string AttendanceStatus { get; set; } = null!;

        public decimal Price { get; set; }

        public decimal DepositAmount { get; set; }

        public string DepositStatus { get; set; } = null!;
    }
}
