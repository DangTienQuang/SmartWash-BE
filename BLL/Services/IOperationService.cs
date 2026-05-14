using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoWashPro.BLL.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoWashPro.BLL.Services
{
    public interface IOperationService
    {
        Task<List<ServiceResponseDTO>> GetServicesAsync();
        Task<BookingResponseDTO> CreateBookingAsync(int userId, CreateBookingDTO request);
        Task<List<BookingResponseDTO>> GetMyBookingsAsync(int userId);
        Task<ServiceResponseDTO> CreateServiceAsync(CreateServiceDTO request);
    }
}