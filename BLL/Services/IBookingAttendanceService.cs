using BLL.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services.Interface
{
    public interface IBookingAttendanceService
    {
        Task CheckInVehicleAsync(VehicleCheckInDTO dto);

        Task CompleteVehicleAsync(VehicleCompleteDTO dto);

        Task MarkNoShowAsync(VehicleNoShowDTO dto);
    }
}
