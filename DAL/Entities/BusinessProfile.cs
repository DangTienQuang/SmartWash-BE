using AutoWashPro.DAL.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities
{
    public class BusinessProfile
    {
        public int BusinessProfileId { get; set; }
        [ForeignKey("User")]
        public int UserId { get; set; }
        [Required]
        [MaxLength(100)]
        public string CompanyName { get; set; } = null!;

        public string? TaxCode { get; set; }

        public string? BusinessAddress { get; set; }

        public string? BillingEmail { get; set; }

        public string? RepresentativeName { get; set; }

        public int? PaymentTermDays { get; set; }

        public DateTime CreatedAt { get; set; }

        public User User { get; set; } = null!;
    }

}
