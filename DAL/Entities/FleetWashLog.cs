using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities
{
    public class FleetWashLog
    {
        public int FleetWashLogId { get; set; }

        public int FleetVehicleId { get; set; }

        public int BranchId { get; set; }

        public int BookingDetailId { get; set; }

        public DateTime CheckInTime { get; set; }

        public DateTime? CompletedTime { get; set; }

        public decimal WashCost { get; set; }
    }
}
