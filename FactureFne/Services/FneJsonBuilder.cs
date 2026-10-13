using FactureFne.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace FactureFne.Services
{
    public static class FneJsonBuilder
    {

        public static string BuildInvoiceJson(AppState state, InvoiceInfo invoice, List<InvoiceItem> items, ClientInfo client)
        {
            //  Taxes globales saisies pour toute la facture
            var globalCustomTaxes = new List<object>();
            if (invoice.GlobalCustomTaxes != null && invoice.GlobalCustomTaxes.Any())
            {
                globalCustomTaxes = invoice.GlobalCustomTaxes
                    .Select(t => (object)new { name = t.Name, amount = t.Amount })
                    .ToList();
            }

            //  Mapping des articles
            var itemsObj = items.Select(i => new
            {
                reference = i.CodeArticle,
                description = i.Libelle,
                quantity = i.Quantite,
                amount = i.PrixUnitaire,
                discount = i.Remise,
                measurementUnit = i.Unite,
                taxes = string.IsNullOrEmpty(i.TypeTaxe) ? new List<string>() : new List<string> { i.TypeTaxe },
                customTaxes = i.CustomTaxes?.Select(t => (object)new { name = t.Name, amount = t.Amount }).ToList()
                              ?? new List<object>()
            }).ToList();

            //  Construction du payload final
            var payload = new
            {
                // Infos facture
                invoiceType = state.Invoice.InvoiceType,
                paymentMethod = state.Invoice.PaymentMethod,
                template = state.Invoice.Template,
                isRne = state.Invoice.IsRne,
                rne = state.Invoice.Rne,

                invoiceNumber = invoice.NumeroFacture,
                invoiceDate = invoice.DateFacture,
                reference = invoice.ReferenceFacture,
                parentReference = invoice.ParentReference,
                totalHt = invoice.MontantHtTotal,
                totalTva = invoice.MontantTvaTotal,

                // Infos client
                clientNcc = client?.Cc ?? "",
                clientCompanyName = client?.Libelle ?? "",
                clientPhone = client?.Tel ?? "",
                clientEmail = client?.Email ?? "",
                clientSellerName = state.Company.ClientSellerName,

                // Infos société
                pointOfSale = state.Company.PointOfSale,
                establishment = state.Company.Establishment,
                commercialMessage = state.Company.CommercialMessage,
                footer = state.Company.Footer,
                foreignCurrency = state.Company.ForeignCurrency,
                foreignCurrencyRate = state.Company.ForeignCurrencyRate,

                // Articles
                items = itemsObj,

                // Taxes globales particulières (insérées dans InvoiceGlobalTaxes)
                customTaxes = globalCustomTaxes,

                // Remise globale
                discount = invoice.Remisetht, 
            };

            return JsonConvert.SerializeObject(payload, Formatting.Indented);
        }

    }
} 

//public static string BuildInvoiceJson(AppState state, InvoiceInfo invoice, List<InvoiceItem> items, ClientInfo client)
  //{
  //    //  Mapping des items avec leurs taxes
  //    var itemsObj = items.Select(i => new
  //    {
  //        reference = i.CodeArticle,
  //        description = i.Libelle,
  //        quantity = i.Quantite,
  //        amount = i.PrixUnitaire,
  //        discount = i.Remise,
  //        measurementUnit = i.Unite,
  //        taxes = new List<string> { i.TypeTaxe },
  //        customTaxes = new List<object>() // par défaut vide, rempli plus bas si besoin
  //    }).ToList();

//    //  Taxes globales personnalisées
//    var globalCustomTaxes = new List<object>();

//    // Vérification condition AIRSI
//    if (state?.Company != null && state.Company.CodcliAirsi == "3" && client != null && client.Valeur == 3m)
//    {
//        globalCustomTaxes.Add(new { name = "AIRSI", amount = 5m }); // 5% standard
//    }

//    //  Construction du payload final
//    var payload = new
//    {
//        invoiceType = state.Invoice.InvoiceType,
//        paymentMethod = state.Invoice.PaymentMethod,
//        template = state.Invoice.Template,
//        isRne = state.Invoice.IsRne,
//        rne = state.Invoice.Rne,

//        // Infos client
//        clientNcc = client?.Cc ?? "",
//        clientCompanyName = client?.Libelle ?? "",
//        clientPhone = client?.Tel ?? "",
//        clientEmail = client?.Email ?? "",
//        clientSellerName = state.Company.ClientSellerName,

//        // Infos société
//        pointOfSale = state.Company.PointOfSale,
//        establishment = state.Company.Establishment,
//        commercialMessage = state.Company.CommercialMessage,
//        footer = state.Company.Footer,
//        foreignCurrency = state.Company.ForeignCurrency,
//        foreignCurrencyRate = state.Company.ForeignCurrencyRate,

//        // Articles
//        items = itemsObj,

//        // Taxes globales
//        customTaxes = globalCustomTaxes,

//        // Remise globale
//        discount = state.Invoice.Discount ?? 0
//    };

//    return JsonConvert.SerializeObject(payload, Formatting.Indented);
//}

