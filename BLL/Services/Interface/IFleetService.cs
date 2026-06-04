using BLL.DTOs.Fleet;
using DAL.Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services.Interface
{
    public interface IFleetService
    {
        Task<FleetImportResultDTO> ImportFleetAsync(int userId, IFormFile file);
        Task<List<FleetImportBatch>> GetImportBatchesAsync();
        Task<FleetImportDetailDTO> GetImportBatchDetailAsync(int batchId);
        Task<List<FleetVehicleDTO>> GetPendingVehiclesAsync(int businessUserId);
        Task ApproveFleetVehicleAsync(int fleetVehicleId);
        Task RejectFleetVehicleAsync(int fleetVehicleId, string reason);
    }
}
