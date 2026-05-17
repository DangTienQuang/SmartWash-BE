using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoWashPro.DAL.Entities
{
    public class Vehicle
    {
        [Key]
        [MaxLength(20)]
        public required string LicensePlate { get; set; }

        [ForeignKey("User")]
        public int? UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        public int VehicleTypeId { get; set; }

        [ForeignKey("VehicleTypeId")]
        public VehicleType VehicleType { get; set; } = null!;
    }
}