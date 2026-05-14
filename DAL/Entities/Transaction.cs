using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoWashPro.DAL.Entities
{
    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }

        [ForeignKey("Wallet")]
        public int WalletId { get; set; }
        public Wallet Wallet { get; set; }

        [ForeignKey("Booking")]
        public int? BookingId { get; set; }
        public Booking Booking { get; set; }

        public decimal Amount { get; set; }

        [MaxLength(20)]
        public string TransactionType { get; set; }

        public DateTime TransactionDate { get; set; }
    }
}
