using System.Collections.Generic;

namespace FactureFne.Models
{
    public class AppState
    {
        public CompanyInfo Company { get; set; } = new CompanyInfo();
        public ApiSettings Api { get; set; } = new ApiSettings();
        public ClientInfo Client { get; set; } = new ClientInfo();
        public InvoiceDraft Invoice { get; set; } = new InvoiceDraft();
        public User CurrentUser { get; set; }

        // Exemple : stocker l'utilisateur après login
        public void SetCurrentUser(User user)
        {
            CurrentUser = user;
        }
    }

    public class CompanyInfo
    {
        public string PointOfSale { get; set; } = "";       // pointOfSale (O) :contentReference[oaicite:0]{index=0}
        public string Establishment { get; set; } = "";     // establishment (O) :contentReference[oaicite:1]{index=1}
        public string CommercialMessage { get; set; } = ""; // commercialMessage (N) :contentReference[oaicite:2]{index=2}
        public string Footer { get; set; } = "";            // footer (N) :contentReference[oaicite:3]{index=3}

        // Devise & taux – obligatoires pour B2F si devise différente (doc) :contentReference[oaicite:4]{index=4}
        public string ForeignCurrency { get; set; } = "";   // ex: "", XOF, USD, EUR… (annexe) :contentReference[oaicite:5]{index=5}
        public decimal ForeignCurrencyRate { get; set; } = 0m;

        //  Champs supplémentaires
        public string NccEntreprise { get; set; } = "";   // NCC de l'entreprise
        public string ClientSellerName { get; set; } = "";  // NOM du Vendeur
        public string CodcliAirsi { get; set; } = "";     // Code client AIRSI
        public string TauxcliAirsi { get; set; } = "";     // Tauxclient AIRSI


        public bool IsEmpty()
        {
            return string.IsNullOrWhiteSpace(PointOfSale)
                && string.IsNullOrWhiteSpace(Establishment)
                && string.IsNullOrWhiteSpace(CommercialMessage)
                && string.IsNullOrWhiteSpace(Footer)
                && string.IsNullOrWhiteSpace(NccEntreprise)
                && string.IsNullOrWhiteSpace(ClientSellerName)
                && string.IsNullOrWhiteSpace(CodcliAirsi)
                && string.IsNullOrWhiteSpace(ForeignCurrency)
                && ForeignCurrencyRate == 0;
        }

    }

    public class ApiSettings
    {
        public string BaseUrl { get; set; } = "http://54.247.95.108/ws/";
        public string Token { get; set; } = "";
        public int TimeoutSeconds { get; set; } = 30;
    }


    public class ClientInfo
    {
        public int Id { get; set; }
        public string Cc { get; set; } = "";
        public string Compte { get; set; } = "";
        public string Libelle { get; set; } = "";
        public string Tel { get; set; } = "";
        public string Email { get; set; } = "";
        public string CompteComptable { get; set; } = "";
        public decimal Valeur { get; set; }
    }

    public class InvoiceDraft
    {
        public string InvoiceType { get; set; } = "sale";
        public string PaymentMethod { get; set; } = "cash";
        public string Template { get; set; } = "B2B";        // ← ajouté
        public bool IsRne { get; set; } = false;           //  ← ajouté
        public string Rne { get; set; } = "";             // ← ajouté

        public decimal? Discount { get; set; }
        public List<InvoiceItemDraft> Items { get; set; } = new List<InvoiceItemDraft>();
        public List<CustomTaxDraft> CustomTaxes { get; set; } = new List<CustomTaxDraft>();

    }


    public class InvoiceItemDraft
    {
        public string Reference { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Quantity { get; set; }
        public decimal Amount { get; set; }
        public decimal? Discount { get; set; }
        public string MeasurementUnit { get; set; } = "";
        public List<string> Taxes { get; set; } = new List<string>(); // ["TVA"] obligatoire
        public List<CustomTaxDraft> CustomTaxes { get; set; } = new List<CustomTaxDraft>(); // vide
    }

    public class CustomTaxDraft
    {
        public string Name { get; set; } = "";  // O si customTaxes présent :contentReference[oaicite:23]{index=23}
        public decimal Amount { get; set; }     // O si customTaxes présent :contentReference[oaicite:24]{index=24}
    }


}
