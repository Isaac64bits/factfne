using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactureFne.Models
{

    public class CustomTax
    {
        public int Id { get; set; }
        public int InvoiceItemId { get; set; }  // clé étrangère vers InvoiceItem
        public string Name { get; set; }
        public decimal Amount { get; set; }
    }

    public class GlobalCustomTax
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }  // clé étrangère vers Invoice
        public string Name { get; set; }
        public decimal Amount { get; set; }
    }
    internal class TaxeSupplementaire
    {

    }
}
