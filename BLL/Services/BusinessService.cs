using AutoWashPro.BLL.Exceptions;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
using BLL.DTOs;
using BLL.Services.Interface;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services
{
    public class BusinessService : IBusinessService
    {
        private readonly AutoWashDbContext _context;
        private readonly ICloudinaryService _cloudinaryService;

        public BusinessService(AutoWashDbContext context, ICloudinaryService cloudinaryService)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<BusinessProfileResponseDTO> CreateBusinessProfileAsync(int userId, CreateBusinessProfileRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserId == userId);

            if (user == null)
            {
                throw new NotFoundException("User not found.");
            }

            var existingBusiness = await _context.BusinessProfiles.FirstOrDefaultAsync(x => x.UserId == userId);

            if (existingBusiness != null)
            {
                throw new BadRequestException("Business profile already exists.");
            }

            // Upload Business License
            var businessLicenseUrl = await _cloudinaryService.UploadPdfAsync(request.BusinessLicense);

            // Upload Authorization Letter (optional)
            string? authorizationLetterUrl = null;

            if (request.AuthorizationLetter != null)
            {
                authorizationLetterUrl = await _cloudinaryService.UploadPdfAsync(request.AuthorizationLetter);
            }

            var entity = new BusinessProfile
            {
                UserId = userId,
                CompanyName = request.CompanyName,
                TaxCode = request.TaxCode,
                BusinessAddress = request.BusinessAddress,
                BillingEmail = request.BillingEmail,
                RepresentativeName = request.RepresentativeName,
                PaymentTermDays = request.PaymentTermDays,
                ApprovalStatus = "Pending",
                BusinessLicenseFileUrl = businessLicenseUrl,
                AuthorizationLetterFileUrl = authorizationLetterUrl,
                CreatedAt = DateTime.UtcNow
            };

            _context.BusinessProfiles.Add(entity);

            await _context.SaveChangesAsync();

            return new BusinessProfileResponseDTO
            {
                BusinessProfileId = entity.BusinessProfileId,
                CompanyName = entity.CompanyName,
                TaxCode = entity.TaxCode,
                BusinessAddress = entity.BusinessAddress
            };
        }

        public async Task<BusinessProfileResponseDTO?> GetByUserIdAsync(int userId)
        {
            return await _context.BusinessProfiles
                .Where(x => x.UserId == userId)
                .Select(x => new BusinessProfileResponseDTO
                {
                    BusinessProfileId = x.BusinessProfileId,
                    CompanyName = x.CompanyName,
                    TaxCode = x.TaxCode,
                    BusinessAddress = x.BusinessAddress,
                    ApprovalStatus = x.ApprovalStatus,
                    BusinessLicenseFileUrl = x.BusinessLicenseFileUrl,
                    AuthorizationLetterFileUrl = x.AuthorizationLetterFileUrl
                })
                .FirstOrDefaultAsync();
        }

        public async Task<List<BusinessBookingResponseDTO>> GetBusinessBookingsAsync(int userId)
        {
            var businessProfile = await _context.BusinessProfiles
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (businessProfile == null)
            {
                throw new NotFoundException("Business profile not found.");
            }

            var bookings = await _context.Bookings
                .Include(x => x.BookingDetails)
                    .ThenInclude(d => d.Service)
                .Where(x =>
                    x.BusinessProfileId ==
                    businessProfile.BusinessProfileId)
                .OrderByDescending(x => x.ScheduledTime)
                .ToListAsync();

            return bookings.Select(x =>
                new BusinessBookingResponseDTO
                {
                    BookingId = x.BookingId,
                    BookingDate = x.ScheduledTime,
                    BookingStatus = x.Status,
                    TotalAmount = x.FinalAmount,
                    TotalVehicles = x.BookingDetails.Count,
                    Vehicles = x.BookingDetails.Select(d =>
                        new BusinessBookingDetailDTO
                        {
                            DetailId = d.DetailId,
                            LicensePlate = d.LicensePlate,
                            ServiceName = d.Service.ServiceName,
                            AttendanceStatus = d.AttendanceStatus,
                            Price = d.Price,
                            DepositAmount = d.DepositAmount,
                            DepositStatus = d.DepositStatus
                        })
                        .ToList()
                })
                .ToList();
        }

        public async Task<int> CreateBusinessBookingAsync(int userId, CreateBusinessBookingDTO dto)
        {
            var businessProfile = await _context.BusinessProfiles
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (businessProfile == null)
            {
                throw new NotFoundException("Business profile not found.");
            }

            if (businessProfile.ApprovalStatus != "Approved")
            {
                throw new ForbiddenException("Business account has not been approved.");
            }

            if (dto.Vehicles == null || !dto.Vehicles.Any())
            {
                throw new BadRequestException("Vehicle list cannot be empty.");
            }

            decimal totalOriginalPrice = dto.Vehicles.Sum(x => x.Price);

            // Create booking
            var booking = new Booking
            {
                UserId = userId,
                BusinessProfileId = businessProfile.BusinessProfileId,
                BookingType = "Business",
                ScheduledTime = dto.ScheduledTime,
                Status = "Pending",
                OriginalPrice = totalOriginalPrice,
                FinalAmount = totalOriginalPrice,
                CreatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);

            await _context.SaveChangesAsync();

            // Create booking details
            var bookingDetails = dto.Vehicles.Select(v =>
                    new BookingDetail
                    {
                        BookingId = booking.BookingId,
                        LicensePlate = v.LicensePlate,
                        ServiceId = v.ServiceId,
                        Price = v.Price,
                        DepositAmount = v.DepositAmount,
                        DepositStatus = "Reserved",
                        AttendanceStatus = "Pending"
                    })
                .ToList();

            _context.BookingDetails.AddRange(bookingDetails);

            await _context.SaveChangesAsync();

            return booking.BookingId;
        }

        public async Task ReviewBusinessProfileAsync(int reviewerId, ReviewBusinessProfileDTO dto)
        {
            var profile = await _context.BusinessProfiles
                .Include(x => x.User)
                .FirstOrDefaultAsync(x =>
                    x.BusinessProfileId == dto.BusinessProfileId);

            if (profile == null)
            {
                throw new NotFoundException("Business profile not found.");
            }

            if (profile.ApprovalStatus != "Pending")
            {
                throw new BadRequestException("Application already reviewed.");
            }

            profile.ReviewedByUserId = reviewerId;
            profile.ReviewedAt = DateTime.UtcNow;

            if (dto.IsApproved)
            {
                profile.ApprovalStatus = "Approved";
                profile.User.Role = "Business";
            }
            else
            {
                profile.ApprovalStatus = "Rejected";
                profile.RejectionReason = dto.RejectionReason;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<PendingBusinessApplicationDTO>> GetPendingBusinessApplicationsAsync()
        {
            return await _context.BusinessProfiles
                .Where(x => x.ApprovalStatus == "Pending")
                .Select(x => new PendingBusinessApplicationDTO
                {
                    BusinessProfileId = x.BusinessProfileId,
                    CompanyName = x.CompanyName,
                    TaxCode = x.TaxCode,
                    BusinessAddress = x.BusinessAddress,
                    BillingEmail = x.BillingEmail,
                    RepresentativeName = x.RepresentativeName,
                    BusinessLicenseFileUrl = x.BusinessLicenseFileUrl,
                    AuthorizationLetterFileUrl = x.AuthorizationLetterFileUrl,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<PendingBusinessApplicationDTO?> GetBusinessApplicationDetailAsync(int businessProfileId)
        {
            var profile = await _context.BusinessProfiles
                .FirstOrDefaultAsync(x => x.BusinessProfileId == businessProfileId);

            if (profile == null)
            {
                throw new NotFoundException("Business application not found.");
            }

            return new PendingBusinessApplicationDTO
            {
                BusinessProfileId = profile.BusinessProfileId,
                CompanyName = profile.CompanyName,
                TaxCode = profile.TaxCode,
                BusinessAddress = profile.BusinessAddress,
                BillingEmail = profile.BillingEmail,
                RepresentativeName = profile.RepresentativeName,
                ApprovalStatus = profile.ApprovalStatus,
                RejectionReason = profile.RejectionReason,
                BusinessLicenseFileUrl = profile.BusinessLicenseFileUrl,
                AuthorizationLetterFileUrl = profile.AuthorizationLetterFileUrl,
                CreatedAt = profile.CreatedAt
            };
        }
    }
}