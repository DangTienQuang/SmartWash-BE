using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs.Business
{
    public class CreateBusinessBookingDTO
    {
        public List<int> FleetVehicleIds { get; set; } = new();
        public int BranchId { get; set; }
        public int SlotId { get; set; }
        public DateTime ScheduledTime { get; set; }
        public List<int> ServiceIds { get; set; } = new();
    }
}
