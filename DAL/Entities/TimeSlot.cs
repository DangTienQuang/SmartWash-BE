using System;
using System.ComponentModel.DataAnnotations;

namespace AutoWashPro.DAL.Entities
{
    public class TimeSlot
    {
        [Key]
        public int SlotId { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public int MaxCapacity { get; set; } = 3;

        public bool IsVipOnly { get; set; } = false;
    }
}