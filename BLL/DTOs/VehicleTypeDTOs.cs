using System.ComponentModel.DataAnnotations;

namespace AutoWashPro.BLL.DTOs
{
    public class VehicleTypeDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
    }

    public class CreateVehicleTypeDTO
    {
        [Required(ErrorMessage = "Tên loại xe không được để trống.")]
        public string Name { get; set; }

        public string? Description { get; set; }
    }
}