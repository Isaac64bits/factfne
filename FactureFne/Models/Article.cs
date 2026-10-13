using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactureFne.Models
{
    public class Article
    {
        public int Id { get; set; }
        public string Ref_Article { get; set; } = "";
        public string Designation { get; set; } = "";
        public decimal Quantite { get; set; } = 0;  //  nouvelle colonne
        public string Unite { get; set; } = "";
        public decimal Prix { get; set; }
    }
}
