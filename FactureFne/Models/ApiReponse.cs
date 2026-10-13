using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactureFne.Models
{
    public class ApiResponse
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }          // Id interne SQLite
        public string ApiReference { get; set; } = ""; // Référence interne API (Invoice.Reference)
        public string ApiToken { get; set; } = "";
        public string ApiStatus { get; set; } = "";
        public string ApiInvoiceId { get; set; } = "";   //  Ajouté : Id de la facture côté API
        public string ItemsJson { get; set; } = "";
        public string RawResponse { get; set; } = "";
        public DateTime CreatedAt { get; set; } 

        // Helper: récupérer le mapping en objet facilement
        public List<ApiItemMapping> GetItemsMapping()
        {
            if (string.IsNullOrWhiteSpace(ItemsJson)) return new List<ApiItemMapping>();
            try
            {
                return JsonConvert.DeserializeObject<List<ApiItemMapping>>(ItemsJson) ?? new List<ApiItemMapping>();
            }
            catch
            {
                return new List<ApiItemMapping>();
            }
        }
    }

    public class ApiItemMapping
    {
        public int LocalItemId { get; set; }    // Id local dans InvoiceItems (int)
        public string ApiItemId { get; set; }   // id string renvoyé par l'API
        public string Reference { get; set; }   // code/article pour info
        public decimal Quantity { get; set; }   // quantité
    }

    

}
