using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoWashPro.DAL.Entities
{
    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }

        [Required]
        public int WalletId { get; set; }
        [ForeignKey("WalletId")]
        public Wallet Wallet { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(20)]
        public string TransactionType { get; set; }

        public string Description { get; set; }

        public int? ReferenceBookingId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}