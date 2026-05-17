using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.ComponentModel.DataAnnotations;

namespace AutoWashPro.DAL.Entities
{
    public class Voucher
    {
        [Key]
        public int VoucherId { get; set; }

        [Required]
        [MaxLength(50)]
        public required string Code { get; set; }

        public decimal DiscountAmount { get; set; }
        public int MaxUsages { get; set; }

        public DateTime ExpiryDate { get; set; }

        public int PointsRequired { get; set; }
    }
}