using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs
{
    public class CreateBusinessBookingDTO
    {
        public DateTime ScheduledTime { get; set; }

        public List<BusinessVehicleBookingDTO> Vehicles { get; set; } = new();
    }

    public class BusinessVehicleBookingDTO
    {
        public string LicensePlate { get; set; } = null!;

        public int ServiceId { get; set; }

        public decimal Price { get; set; }

        public decimal DepositAmount { get; set; }
    }
}
