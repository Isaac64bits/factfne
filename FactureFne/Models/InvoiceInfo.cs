using System;
using System.Collections.Generic;

namespace FactureFne.Models
{


    public class InvoiceInfo
    {
        public int Id { get; set; }
        public string NumeroFacture { get; set; } = "";   // FC0006959
        public string DateFacture { get; set; } = "";     // 060825
        public string CompteClient { get; set; } = "";    // 411108970
        public string ReferenceFacture { get; set; } = ""; // FACT 1963 
        public string ParentReference { get; set; } = ""; // 25000000142
        public DateTime ImportDate { get; set; } = DateTime.Now;
        public decimal MontantHtTotal { get; set; }
        public decimal MontantTvaTotal { get; set; }
        public string TypeFacture { get; set; } = "";   // "6" = VENTE, "7" = AVOIR
        public decimal Remisetht { get; set; }
        public bool IsCertified { get; set; }

        // Taxes particulières GLOBALES (niveau facture) — nouveau nom clair
        public List<CustomTaxDraft> GlobalCustomTaxes { get; set; } = new List<CustomTaxDraft>();

        //public static implicit operator InvoiceInfo(InvoiceInfo v)
        //{
        //    throw new NotImplementedException();
        //}
    }


}
