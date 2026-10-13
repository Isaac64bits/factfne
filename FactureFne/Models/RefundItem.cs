using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactureFne.Models
{
    public class RefundItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }       // ID de l’article d’origine (venant de la facture certifiée)

        [JsonProperty("quantity")]
        public int Quantity { get; set; } // Quantité à retourner
    }
}