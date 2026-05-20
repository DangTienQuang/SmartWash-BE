using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AutoWashPro.BLL.DTOs
{
    public class UserProfileDTO
    {
        public int UserId { get; set; }
        public required string FullName { get; set; }
        public required string PhoneNumber { get; set; }
        public required string TierName { get; set; }
        public double ChurnScore { get; set; }
        public required List<VehicleDTO> Vehicles { get; set; }
    }

    public class VehicleDTO
    {
        public required string LicensePlate { get; set; }
        public required string VehicleType { get; set; }
    }

    public class CreateVehicleDTO
    {
        [Required(ErrorMessage = "Biển số xe không được để trống.")]
        [RegularExpression(@"^[0-9]{2}[A-Z0-9]-[0-9]{3,5}(\.[0-9]{2})?$", ErrorMessage = "Biển số xe không hợp lệ (VD: 51H-123.45).")]
        public required string LicensePlate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại xe.")]
        public int VehicleTypeId { get; set; }
    }

    public class UpdateVehicleDTO
    {
        [Required(ErrorMessage = "Vui lòng chọn loại xe.")]
        public int VehicleTypeId { get; set; }
    }

    public class UpdateUserProfileDTO
    {
        public string? FullName { get; set; }

        [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})$", ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string? PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string? Email { get; set; }
    }

    public class UpdateUserStatusDTO
    {
        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        [RegularExpression("^(Active|Blocked)$", ErrorMessage = "Trạng thái chỉ được phép là 'Active' hoặc 'Blocked'.")]
        public required string Status { get; set; }
    }

    public class PagedResultDTO<T>
    {
        public required List<T> Items { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
    }

    public class UserAdminSummaryDTO
    {
        public int UserId { get; set; }
        public required string FullName { get; set; }
        public required string PhoneNumber { get; set; }
        public required string TierName { get; set; }
        public required string Status { get; set; }
        public DateTime? LastVisitDate { get; set; }
    }

    public class VehicleRecognitionDTO
    {
        public required string LicensePlate { get; set; }
        public required string VehicleType { get; set; }
        public required string OwnerName { get; set; }
        public required string OwnerPhone { get; set; }
        public required string TierName { get; set; }
        public bool HasActiveBooking { get; set; }
        public int? ActiveBookingId { get; set; }
        public DateTime? ScheduledTime { get; set; }
    }
}