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
    public class PointLedger
    {
        [Key]
        public int LedgerId { get; set; }

        [ForeignKey("Wallet")]
        public int WalletId { get; set; }
        public Wallet Wallet { get; set; }

        public int PointsAdded { get; set; }
        public int PointsRemaining { get; set; }

        public DateTime EarnedDate { get; set; }
        public DateTime ExpirationDate { get; set; }
    }
}
