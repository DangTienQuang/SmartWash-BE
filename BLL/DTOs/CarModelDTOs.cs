using System.ComponentModel.DataAnnotations;

namespace AutoWashPro.BLL.DTOs
{
    public class CarModelDTO
    {
        public int Id { get; set; }
        public string Brand { get; set; }
        public string Name { get; set; }

        [StringLength(100, ErrorMessage = "Model version must not exceed 100 characters.")]
        public string? ModelVersion { get; set; }

        [Range(1900, 2100, ErrorMessage = "Manufacture year is invalid.")]
        public int? ManufactureYear { get; set; }
    }

    public class CreateCarModelDTO
    {
        public string Brand { get; set; }
        public string Name { get; set; }

        [StringLength(100, ErrorMessage = "Model version must not exceed 100 characters.")]
        public string? ModelVersion { get; set; }

        [Range(1900, 2100, ErrorMessage = "Manufacture year is invalid.")]
        public int? ManufactureYear { get; set; }
    }

    public class UpdateCarModelDTO
    {
        public string Brand { get; set; }
        public string Name { get; set; }

        [StringLength(100, ErrorMessage = "Model version must not exceed 100 characters.")]
        public string? ModelVersion { get; set; }

        [Range(1900, 2100, ErrorMessage = "Manufacture year is invalid.")]
        public int? ManufactureYear { get; set; }
        public bool IsActive { get; set; }
    }
}
