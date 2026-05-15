using System.Collections.Generic;
using System.Threading.Tasks;
using AutoWashPro.BLL.DTOs;

namespace AutoWashPro.BLL.Services
{
    public interface IOperationService
    {
        Task<BookingResponseDTO> CreateBookingAsync(int userId, CreateBookingDTO request);
        Task<List<BookingResponseDTO>> GetMyBookingsAsync(int userId);
    }
}