using Dapper;
using FactureFne.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace FactureFne.Services
{
    public class ApiResponseRepository
    {
        private readonly string _dbPath;

        public ApiResponseRepository(string dbPath = null)
        {
            _dbPath = string.IsNullOrWhiteSpace(dbPath)
                ? DatabaseHelper.GetDatabasePath("apiResponses.db")
                : dbPath;

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            if (!File.Exists(_dbPath))
                    //File.Delete(_dbPath); //  reset complet
                SQLiteConnection.CreateFile(_dbPath);


            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();

                connection.Execute(@"
                    CREATE TABLE IF NOT EXISTS ApiResponses (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        InvoiceId INTEGER,
                        ApiReference TEXT,
                        ApiToken TEXT,
                        ApiStatus TEXT,
                        ApiInvoiceId TEXT,
                        ItemsJson TEXT,
                        RawResponse TEXT,
                        CreatedAt DATETIME
                    );
                ");
            }
        }

        public void Save(ApiResponse response)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();
                connection.Execute(@"
                    INSERT INTO ApiResponses
                        (InvoiceId, ApiReference, ApiToken, ApiStatus, ApiInvoiceId, ItemsJson, RawResponse, CreatedAt)
                    VALUES
                        (@InvoiceId, @ApiReference, @ApiToken, @ApiStatus, @ApiInvoiceId, @ItemsJson, @RawResponse, @CreatedAt)",
                    response);
            }
        }

    
        // Construit et sauvegarde un ApiResponse à partir de la réponse API et des items locaux.
       
        public void SaveFromApiResponse(int localInvoiceId, ApiInvoiceResponse apiResponse, IEnumerable<InvoiceItem> localItems)
        {
            if (apiResponse?.Invoice == null)
                throw new ArgumentNullException(nameof(apiResponse), "Réponse API invalide (Invoice manquant)");

            var localList = localItems?.Select(li => new LocalItemLight
            {
                LocalId = li.Id,
                Reference = li.CodeArticle?.Trim() ?? "",
                Quantity = li.Quantite
            }).ToList() ?? new List<LocalItemLight>();

            var mappings = new List<ApiItemMapping>();
            var usedLocal = new HashSet<int>();

            //  Parcourir les items renvoyés par l’API
            foreach (var apiItem in apiResponse.Invoice.Items ?? new List<ItemDetail>())
            {
                string apiRef = apiItem.Reference?.Trim() ?? "";
                decimal apiQty = apiItem.Quantity;

                // 1. Tentative stricte : Reference + Quantité
                var match = localList.FirstOrDefault(l =>
                    !usedLocal.Contains(l.LocalId) &&
                    string.Equals(l.Reference, apiRef, StringComparison.OrdinalIgnoreCase) &&
                    l.Quantity == apiQty);

                // 2. Tentative assouplie : Reference seule
                if (match == null)
                {
                    match = localList.FirstOrDefault(l =>
                        !usedLocal.Contains(l.LocalId) &&
                        string.Equals(l.Reference, apiRef, StringComparison.OrdinalIgnoreCase));
                }

                if (match != null)
                {
                    usedLocal.Add(match.LocalId);
                    mappings.Add(new ApiItemMapping
                    {
                        LocalItemId = match.LocalId,
                        ApiItemId = apiItem.Id,
                        Reference = apiRef,
                        Quantity = apiQty
                    });
                }
                else
                {
                    // Pas trouvé localement → on garde quand même l’info API
                    mappings.Add(new ApiItemMapping
                    {
                        LocalItemId = 0,
                        ApiItemId = apiItem.Id,
                        Reference = apiRef,
                        Quantity = apiQty
                    });
                }
            }

            // Créer la réponse à sauvegarder
            var toSave = new ApiResponse
            {
                InvoiceId = localInvoiceId,
                ApiReference = apiResponse.Reference ?? apiResponse.Invoice.Reference ?? "",
                ApiToken = apiResponse.Token ?? "",
                ApiStatus = apiResponse.Invoice?.Status ?? "",
                ApiInvoiceId = apiResponse.Invoice?.Id ?? "",
                ItemsJson = JsonConvert.SerializeObject(mappings, Formatting.Indented),
                RawResponse = JsonConvert.SerializeObject(apiResponse, Formatting.Indented),
                CreatedAt = DateTime.UtcNow
            };

            // Sauvegarde finale
            Save(toSave);
        }

        public IEnumerable<ApiResponse> GetAll()
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();
                return connection.Query<ApiResponse>("SELECT * FROM ApiResponses ORDER BY CreatedAt DESC");
            }
        }

        public ApiResponse GetByInvoiceId(int invoiceId)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();
                return connection.QueryFirstOrDefault<ApiResponse>("SELECT * FROM ApiResponses WHERE InvoiceId=@InvoiceId ORDER BY CreatedAt DESC", new { InvoiceId = invoiceId });
            }
        }

        public ApiResponse GetByApiInvoiceId(string apiInvoiceId)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();
                return connection.QueryFirstOrDefault<ApiResponse>("SELECT * FROM ApiResponses WHERE ApiInvoiceId=@ApiInvoiceId ORDER BY CreatedAt DESC", new { ApiInvoiceId = apiInvoiceId });
            }
        }

        public ApiResponse GetByApiReference(string apiReference)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();
                return connection.QueryFirstOrDefault<ApiResponse>(
                    "SELECT * FROM ApiResponses WHERE ApiReference=@ApiReference ORDER BY CreatedAt DESC LIMIT 1",
                    new { ApiReference = apiReference });
            }
        }

        public ApiResponse GetByPartialReference(string partialRef)
        {
            if (string.IsNullOrWhiteSpace(partialRef))
                return null;

            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();

                // Ici on récupère la première facture dont la référence API se termine par partialRef
                return connection.QueryFirstOrDefault<ApiResponse>(
                    "SELECT * FROM ApiResponses WHERE ApiReference LIKE @pattern ORDER BY CreatedAt DESC LIMIT 1",
                    new { pattern = "%" + partialRef });
            }
        }

        public IEnumerable<ApiResponse> GetByDateRange(DateTime startUtc, DateTime endUtc)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();

                string sql = @"
                SELECT * 
                FROM ApiResponses
                WHERE CreatedAt BETWEEN @Start AND @End
                ORDER BY CreatedAt DESC;";

                return connection.Query<ApiResponse>(sql, new { Start = startUtc, End = endUtc });
            }
        }



        public void Delete(int id)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();
                connection.Execute("DELETE FROM ApiResponses WHERE Id=@Id", new { Id = id });
            }
        }

        public void DeleteByInvoiceId(int invoiceId)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();
                connection.Execute("DELETE FROM ApiResponses WHERE InvoiceId = @InvoiceId", new { InvoiceId = invoiceId });
            }
        }


    }
}
