using AutoWashPro.BLL.Exceptions;
using AutoWashPro.DAL.Data;
using BLL.DTOs.Fleet;
using BLL.Services.Interface;
using DAL.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class FleetService : IFleetService
    {
        private readonly AutoWashDbContext _context;
        private readonly ICloudinaryService _cloudinaryService;

        public FleetService(AutoWashDbContext context, ICloudinaryService cloudinaryService)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<FleetImportResultDTO> ImportFleetAsync(int userId, IFormFile file)
        {
            var business = await _context.BusinessProfiles.FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.ApprovalStatus == "Approved");

            if (business == null)
            {
                throw new BadRequestException("Business account is not approved.");
            }

            if (file == null || file.Length == 0)
            {
                throw new BadRequestException("Excel file is required.");
            }

            var fileUrl =
                await _cloudinaryService.UploadFileAsync(file, "fleet-imports");

            var batch = new FleetImportBatch
            {
                BusinessProfileId = business.BusinessProfileId,
                FileUrl = fileUrl,
                Status = "Processing",
                CreatedAt = DateTime.UtcNow
            };

            _context.FleetImportBatches.Add(batch);

            await _context.SaveChangesAsync();

            using var stream = new MemoryStream();

            await file.CopyToAsync(stream);

            stream.Position = 0;

            using var package = new ExcelPackage(stream);

            var worksheet = package.Workbook.Worksheets[0];

            int rowCount = worksheet.Dimension.Rows;
            if (worksheet?.Dimension == null)
            {
                throw new BadRequestException("Excel file is empty.");
            }

            var importedPlates = new HashSet<string>();

            for (int row = 2; row <= rowCount; row++)
            {
                string licensePlate = worksheet.Cells[row, 2].Text.Trim();
                string vehicleTypeName = worksheet.Cells[row, 3].Text.Trim();
                string brand = worksheet.Cells[row, 4].Text.Trim();
                string model = worksheet.Cells[row, 5].Text.Trim();
                string driverName = worksheet.Cells[row, 6].Text.Trim();
                string employeeCode = worksheet.Cells[row, 7].Text.Trim();

                var errors = new List<string>();

                if (string.IsNullOrWhiteSpace(licensePlate))
                {
                    errors.Add("License plate is required.");
                }

                if (importedPlates.Contains(licensePlate))
                {
                    errors.Add("Duplicate license plate.");
                }

                bool existed = await _context.FleetVehicles.AnyAsync(x => x.LicensePlate == licensePlate);
                if (existed)
                {
                    errors.Add("License plate already exists in system.");
                }

                importedPlates.Add(licensePlate);

                var vehicleType = await _context.VehicleTypes.FirstOrDefaultAsync(x => x.Name == vehicleTypeName);

                if (vehicleType == null)
                {
                    errors.Add($"Vehicle type '{vehicleTypeName}' not found.");
                }

                if (errors.Any())
                {
                    foreach (var error in errors)
                    {
                        _context.FleetImportErrors.Add(
                            new FleetImportError
                            {
                                FleetImportBatchId = batch.FleetImportBatchId,
                                RowNumber = row,
                                ErrorMessage = error
                            });
                    }

                    batch.FailedRows++;

                    continue;
                }

                var fleetVehicle = new FleetVehicle
                {
                    BusinessProfileId = business.BusinessProfileId,
                    FleetImportBatchId= batch.FleetImportBatchId,
                    LicensePlate = licensePlate,
                    VehicleTypeId = vehicleType!.Id,
                    Brand = brand,
                    Model = model,
                    DriverName = driverName,
                    EmployeeCode = employeeCode,
                    Status = "PendingApproval",
                    CreatedAt = DateTime.UtcNow
                };

                _context.FleetVehicles.Add(fleetVehicle);

                batch.SuccessRows++;
            }

            batch.TotalRows = batch.SuccessRows + batch.FailedRows;

            if (batch.SuccessRows == 0)
            {
                batch.Status = "Failed";
            }
            else if (batch.FailedRows > 0)
            {
                batch.Status = "PartialSuccess";
            }
            else
            {
                batch.Status = "Completed";
            }

            await _context.SaveChangesAsync();

            return new FleetImportResultDTO
            {
                FleetImportBatchId = batch.FleetImportBatchId,
                TotalRows = batch.TotalRows,
                SuccessRows = batch.SuccessRows,
                FailedRows = batch.FailedRows,
                Status = batch.Status
            };
        }

        public async Task<List<FleetImportBatch>> GetImportBatchesAsync()
        {
            return await _context.FleetImportBatches
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<FleetImportDetailDTO> GetImportBatchDetailAsync(int batchId)
        {
            var batch = await _context.FleetImportBatches.FirstOrDefaultAsync(x => x.FleetImportBatchId == batchId);

            if (batch == null)
            {
                throw new NotFoundException("Import batch not found.");
            }

            var errors = await _context.FleetImportErrors
                    .Where(x => x.FleetImportBatchId == batchId)
                    .Select(x =>
                        new FleetImportErrorDTO
                        {
                            RowNumber = x.RowNumber,
                            ErrorMessage = x.ErrorMessage
                        })
                    .ToListAsync();

            return new FleetImportDetailDTO
            {
                FleetImportBatchId = batch.FleetImportBatchId,
                Status = batch.Status,
                TotalRows = batch.TotalRows,
                SuccessRows = batch.SuccessRows,
                FailedRows = batch.FailedRows,
                Errors = errors
            };
        }

        public async Task<List<FleetVehicleDTO>> GetPendingVehiclesAsync(int businessUserId)
        {
            var business = await _context.BusinessProfiles.FirstOrDefaultAsync(x => x.UserId == businessUserId);

            if (business == null)
            {
                throw new NotFoundException("Business profile not found.");
            }

            return await _context.FleetVehicles
                .Include(x => x.VehicleType)
                .Where(x =>
                    x.BusinessProfileId ==
                    business.BusinessProfileId &&
                    x.Status == "PendingApproval")
                .Select(x => new FleetVehicleDTO
                {
                    FleetVehicleId = x.FleetVehicleId,
                    LicensePlate = x.LicensePlate,
                    Brand = x.Brand,
                    Model = x.Model,
                    VehicleTypeName = x.VehicleType.Name,
                    DriverName = x.DriverName,
                    EmployeeId = x.EmployeeCode,
                    Status = x.Status
                })
                .ToListAsync();
        }

        public async Task ApproveFleetVehicleAsync(int fleetVehicleId)
        {
            var vehicle = await _context.FleetVehicles.FirstOrDefaultAsync(x => x.FleetVehicleId == fleetVehicleId);

            if (vehicle == null)
            {
                throw new NotFoundException("Fleet vehicle not found.");
            }

            vehicle.Status = "Active";

            await _context.SaveChangesAsync();
        }

        public async Task RejectFleetVehicleAsync(int fleetVehicleId, string reason)
        {
            var vehicle = await _context.FleetVehicles.FirstOrDefaultAsync(x => x.FleetVehicleId == fleetVehicleId);

            if (vehicle == null)
            {
                throw new NotFoundException("Fleet vehicle not found.");
            }

            vehicle.Status = "Rejected";
            vehicle.RejectionReason = reason;

            await _context.SaveChangesAsync();
        }
    }
}
