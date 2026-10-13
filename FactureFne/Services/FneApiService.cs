using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FactureFne.Models;
using System.Linq;


namespace FactureFne.Services
{
    public class FneApiService
    {
        private readonly HttpClient _http;

        public FneApiService(string baseUrl, string token, int timeoutSeconds = 30)
        {
            _http = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(timeoutSeconds)
            };

            if (!string.IsNullOrWhiteSpace(token))
            {
                _http.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            }
        }

        //Methode d'envoie à la FNE des factures de Vente
        public async Task<string> SendInvoiceAsync(string jsonPayload)
        {
            //  Ici on force l’encodage en UTF-8
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _http.PostAsync("external/invoices/sign", content);

            string result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Erreur API ({response.StatusCode}): {result}");
            }

            return result;
        }

        //Methode d'envoie à la FNE des factures d'Avoir
        public async Task<string> SendRefundAsync(string parentInvoiceId, List<RefundItem> refundItems)
        {
            if (string.IsNullOrWhiteSpace(parentInvoiceId))
                throw new ArgumentException("Parent invoice ID manquant pour l’avoir.");

            var filteredItems = refundItems.Where(i => i.Quantity > 0).ToList();
            if (filteredItems.Count == 0)
                throw new Exception("Aucun article valide à rembourser.");

            var payload = new { items = filteredItems };

            string jsonPayload = JsonConvert.SerializeObject(payload, Formatting.Indented,
                new JsonSerializerSettings { Culture = System.Globalization.CultureInfo.InvariantCulture });

            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _http.PostAsync($"external/invoices/{parentInvoiceId}/refund", content);

            string result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Erreur API Refund ({response.StatusCode}) pour la facture {parentInvoiceId}: {result}");

            return result;
        }

    }
}
