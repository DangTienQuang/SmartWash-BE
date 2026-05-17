using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AutoWashPro.DAL.Entities
{
    public class Service
    {
        [Key]
        public int ServiceId { get; set; }

        [Required]
        [MaxLength(100)]
        public required string ServiceName { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public ICollection<ServicePrice> ServicePrices { get; set; } = new List<ServicePrice>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}