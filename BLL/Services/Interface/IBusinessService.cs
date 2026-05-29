using BLL.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services.Interface
{
    public interface IBusinessService
    {
        Task<BusinessProfileResponseDTO> CreateBusinessProfileAsync(int userId, CreateBusinessProfileDTO dto);

        Task<BusinessProfileResponseDTO?> GetByUserIdAsync(int userId);
        Task<List<BusinessBookingResponseDTO>>GetBusinessBookingsAsync(int userId);
        Task<int> CreateBusinessBookingAsync(int userId, CreateBusinessBookingDTO dto);
    }
}