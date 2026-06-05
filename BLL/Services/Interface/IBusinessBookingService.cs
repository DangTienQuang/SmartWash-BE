using BLL.DTOs.Business;
using BLL.DTOs.Fleet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services.Interface
{
    public interface IBusinessBookingService
    {
        Task<BusinessBookingResponseDTO> CreateBookingAsync(int businessUserId, CreateBusinessBookingDTO dto);
        Task<List<FleetVehicleDTO>> GetActiveFleetVehiclesAsync(int businessUserId);
        Task<List<BusinessBookingListDTO>> GetBookingsAsync(int businessUserId);
        Task<BusinessBookingDetailDTO> GetBookingDetailAsync(int businessUserId, int bookingId);
        Task CancelBookingAsync(int businessUserId, int bookingId);
        Task<FleetWashLogDTO> CheckInAsync(int bookingId);
        Task CompleteWashAsync(int fleetWashLogId);
        Task<FleetCheckInResponseDTO> WalkInAsync(FleetWalkInDTO dto);
        Task WalkOutAsync(int washLogId);
        Task StartProcessingAsync(int washLogId, int staffUserId, StartFleetWashDTO dto);
    }
}
