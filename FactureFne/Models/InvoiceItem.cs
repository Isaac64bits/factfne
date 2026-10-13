using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactureFne.Models
{
    public class InvoiceItem
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public string CodeArticle { get; set; } = "";    
        public string Libelle { get; set; } = "";        
        public decimal Quantite { get; set; }            
        public string Unite { get; set; } = "";         
        public decimal PrixUnitaire { get; set; }       
        public decimal Remise { get; set; }             
        public string TypeTaxe { get; set; } = "";          
        public decimal TauxTva { get; set; }             

        //  Ajout de la liste de taxes personnalisées par ligne
        public List<CustomTaxDraft> CustomTaxes { get; set; } = new List<CustomTaxDraft>();
    }

    class LocalItemLight
    {
        public int LocalId { get; set; }
        public string Reference { get; set; }
        public decimal Quantity { get; set; }
    }

}
