using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AutoWashPro.BLL.DTOs
{
    public class ServiceDTO
    {
        public int ServiceId { get; set; }
        public required string ServiceName { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public required List<ServicePriceDTO> Prices { get; set; }
    }

    public class ServicePriceDTO
    {
        public int VehicleTypeId { get; set; }
        public required string VehicleTypeName { get; set; }
        public decimal Price { get; set; }
        public int DurationMinutes { get; set; }
    }
    public class CreateOrUpdateServiceDTO
    {
        [Required(ErrorMessage = "Tên dịch vụ không được để trống.")]
        public required string ServiceName { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Phải cấu hình ít nhất 1 mức giá cho dịch vụ.")]
        [MinLength(1, ErrorMessage = "Phải cấu hình ít nhất 1 mức giá cho dịch vụ.")]
        public required List<CreateServicePriceDTO> Prices { get; set; }
    }

    public class CreateServicePriceDTO
    {
        [Required]
        public int VehicleTypeId { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Giá tiền không hợp lệ.")]
        public decimal Price { get; set; }

        [Required]
        [Range(5, 600, ErrorMessage = "Thời gian thực hiện (phút) phải từ 5 đến 600.")]
        public int DurationMinutes { get; set; }
    }
}