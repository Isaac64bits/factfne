using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactureFne.Models
{
    public class ApiRefundResponse
    {
        public string Ncc { get; set; }
        public string Reference { get; set; }
        public string Token { get; set; }
        public bool Warning { get; set; }
        public decimal BalanceSticker { get; set; }
    }

}
