using Dapper;
using FactureFne.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace FactureFne.Services
{
    public class InvoiceRepository
    {
        private decimal ParseDecimalRobuste(string s)
        {
            s = s?.Trim();
            if (string.IsNullOrEmpty(s)) return 0m;

            // Essayer d'abord fr-FR (virgule comme séparateur décimal)
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.GetCultureInfo("fr-FR"), out var val))
                return val;

            // Sinon normaliser (enlève séparateurs milliers, remplace virgule par point)
            var norm = s.Replace(" ", "").Replace(".", "").Replace(",", ".");
            if (decimal.TryParse(norm, NumberStyles.Number | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out val))
                return val;

            return 0m; // fallback si rien ne marche
        }

       private string Clean(string s) => s?.Trim();


        private string GetTaxeFromTaux(decimal taux)
        {
                
            switch (taux)
            {
                case 18.0m: return "TVA";
                case 9.0m: return "TVAB";
                case 0.0m: return "TVAD";
                default: return "TVAC"; // si ce n’est pas dans la liste
            }
        }

        private decimal GetTauxFromTaxe(string taxe)
        {
            switch (taxe?.Trim().ToUpper())
            {
                case "TVA": return 18.0m;
                case "TVAB": return 9.0m;
                case "TVAC": return 0.0m;
                case "TVAD": return 0.0m;
                default: return 0.0m; // valeur par défaut si non reconnu
            }
        }


        private string GetTaxeGlobal(decimal taux)
        {

            switch (taux)
            {
                case 18.0m: return "TVA";
                case 9.0m: return "TVAB";
                case 5.0m: return "AIRSI";
                case 0.0m: return "TVAD";
                default: return "TVAC"; // si ce n’est pas dans la liste
            }
        }

        private string GetTypeFacture(string code)
        {
            switch (code)
            {
                case "6":
                    return "VENTE";
                case "7":
                    return "AVOIR";
                default:
                    return "";
            }
        }


        private readonly string _dbPath;

        public InvoiceRepository(string dbPath = null)
        {
            _dbPath = string.IsNullOrWhiteSpace(dbPath)
                ? DatabaseHelper.GetDatabasePath("invoices.db")
                : dbPath;

            InitializeDatabase();
        }


        private void InitializeDatabase()
        {
            //File.Delete(_dbPath);
            bool isNew = !File.Exists(_dbPath);

            if (isNew)
            {
                SQLiteConnection.CreateFile(_dbPath);
            }

            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();
                connection.Execute("PRAGMA encoding = 'UTF-8';");

                // Toujours vérifier/créer les tables
                connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Invoices (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TypeFacture TEXT,
                NumeroFacture TEXT NOT NULL,
                DateFacture TEXT,
                CompteClient TEXT,
                ReferenceFacture TEXT,
                ParentReference TEXT,
                ImportDate TEXT,
                MontantHtTotal REAL,
                MontantTvaTotal REAL,
                Remisetht REAL DEFAULT 0,
                IsCertified INTEGER DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS InvoiceItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                InvoiceId INTEGER NOT NULL,
                CodeArticle TEXT,
                Libelle TEXT,
                Quantite REAL,
                Unite TEXT,
                PrixUnitaire REAL,
                Remise REAL,
                TypeTaxe TEXT,
                TauxTva REAL,
                FOREIGN KEY(InvoiceId) REFERENCES Invoices(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS CustomTaxes (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                InvoiceItemId INTEGER NOT NULL,
                Name TEXT,
                Amount REAL,
                FOREIGN KEY(InvoiceItemId) REFERENCES InvoiceItems(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS GlobalCustomTaxes (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                InvoiceId INTEGER NOT NULL,
                Name TEXT,
                Amount REAL,
                FOREIGN KEY(InvoiceId) REFERENCES Invoices(Id) ON DELETE CASCADE
            );
            ");
            }
        }


        private static Encoding DetectEncoding(string filePath)
        {
            // Essaie UTF-8 strict
            try
            {
                using (var reader = new StreamReader(filePath, new UTF8Encoding(false, true)))
                {
                    while (!reader.EndOfStream)
                        reader.ReadLine(); // si une erreur survient => pas UTF-8
                }
                return new UTF8Encoding(false);
            }
            catch
            {
                return Encoding.GetEncoding(1252); // fallback Windows-1252
            }
        }

        private static string RemoveControlAndNormalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            // Normalisation Unicode (NFC)
            s = s.Normalize(System.Text.NormalizationForm.FormC);
            // supprimer caractères de contrôle invisibles
            var filtered = new string(s.Where(c => !char.IsControl(c) || c == '\r' || c == '\n' || c == '\t').ToArray());
            return filtered.Trim();
        }

        public void ImportFromTxt(string filePath, out int added, out int duplicates, out int qte)
        {
            added = 0;
            duplicates = 0;
            qte = 0;

            Encoding encoding = DetectEncoding(filePath);
            var factures = new Dictionary<string, List<string[]>>();

            //  Lecture du fichier TXT
            using (var reader = new StreamReader(filePath, encoding, detectEncodingFromByteOrderMarks: true))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split(';');
                    if (parts.Length < 13) continue;

                    string numeroFacture = parts[1].Trim();
                    if (!factures.ContainsKey(numeroFacture))
                        factures[numeroFacture] = new List<string[]>();

                    for (int i = 0; i < parts.Length; i++)
                        parts[i] = Clean(RemoveControlAndNormalize(parts[i]));

                    factures[numeroFacture].Add(parts);
                }
            }

            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();
                connection.Execute("PRAGMA encoding = 'UTF-8';");

                using (var tx = connection.BeginTransaction())
                {
                    foreach (var entry in factures)
                    {
                        string numeroFacture = entry.Key;

                        int count = connection.ExecuteScalar<int>(
                            "SELECT COUNT(1) FROM Invoices WHERE NumeroFacture=@NumeroFacture",
                            new { NumeroFacture = numeroFacture },
                            transaction: tx
                        );

                        if (count > 0)
                        {
                            duplicates++;
                            continue;
                        }

                        var lignes = entry.Value;
                        string typeFacture = GetTypeFacture(lignes[0][0]);
                        string dateFacture = lignes[0][2];
                        string compteClient = lignes[0][3];
                        string referenceFacture = "";
                        string parentReference = "";

                        if (typeFacture == "AVOIR")
                        {
                            referenceFacture = lignes[0][1];
                            parentReference = lignes.Last()[14];
                            //parentReference = lignes[0][14];
                        }
                        else
                        {
                            referenceFacture = lignes
                                .Select(l => l[14])
                                .LastOrDefault(r => !string.IsNullOrEmpty(r)) ?? "";
                        }

                        decimal totalHt = 0, totalTva = 0, remiseTotale = 0;
                        decimal taxeGlobale = 0;
                        
                        int invoiceId = connection.ExecuteScalar<int>(
                            @"INSERT INTO Invoices 
                            (TypeFacture, NumeroFacture, DateFacture, CompteClient, ReferenceFacture, ParentReference, ImportDate, MontantHtTotal, MontantTvaTotal,Remisetht,IsCertified)
                            VALUES (@TypeFacture,@NumeroFacture,@DateFacture,@CompteClient,@ReferenceFacture,@ParentReference,@ImportDate,0,0,0,0);
                            SELECT last_insert_rowid();",
                            new
                            {
                                TypeFacture = typeFacture,
                                NumeroFacture = numeroFacture,
                                DateFacture = dateFacture,
                                CompteClient = compteClient,
                                ReferenceFacture = referenceFacture,
                                ParentReference = parentReference,
                                ImportDate = DateTime.Now.ToString("o")
                            }, transaction: tx);

                        //  Boucle sur les lignes de la facture
                        foreach (var parts in lignes)
                        {
                            decimal quantite = ParseDecimalRobuste(parts[6]);

                            //Cette Boucle permet d'ignorer les lignes avec quantité nulle ou négative

                            //if (quantite <= 0)
                            //{
                            //    qte++;
                            //    continue;
                            //}

                            decimal prixUnitaire = ParseDecimalRobuste(parts[8]);
                            decimal remise = ParseDecimalRobuste(parts[9]);
                            decimal tauxTva = ParseDecimalRobuste(parts[11]);

                            // Lire la taxe globale (si présente) depuis la dernière colonne
                            if (parts.Length >= 14)
                            {
                                var valGlobale = ParseDecimalRobuste(parts[13]);
                                if (valGlobale > 0)
                                    taxeGlobale = valGlobale;
                            }

                            List<CustomTaxDraft> customTaxes = new List<CustomTaxDraft>();
                            if (tauxTva == 5m)
                            {
                                customTaxes.Add(new CustomTaxDraft
                                {
                                    Name = "AIRSI",
                                    Amount = 5m
                                });
                            }

                            string typeTaxe = GetTaxeFromTaux(tauxTva);
                            decimal ligneHt = (quantite * prixUnitaire) - remise;

                            totalHt += ligneHt;
                            totalTva += ligneHt * (tauxTva / 100);

                            int itemId = connection.ExecuteScalar<int>(
                                @"INSERT INTO InvoiceItems 
                                (InvoiceId, CodeArticle, Libelle, Quantite, Unite, PrixUnitaire, Remise, TypeTaxe, TauxTva)
                                VALUES (@InvoiceId,@CodeArticle,@Libelle,@Quantite,@Unite,@PrixUnitaire,@Remise,@TypeTaxe,@TauxTva);
                                 SELECT last_insert_rowid();",
                                new
                                {
                                    InvoiceId = invoiceId,
                                    CodeArticle = parts[4],
                                    Libelle = parts[5],
                                    Quantite = quantite,
                                    Unite = parts[7],
                                    PrixUnitaire = prixUnitaire,
                                    Remise = remise,
                                    TypeTaxe = typeTaxe,
                                    TauxTva = tauxTva
                                }, transaction: tx);

                            // Taxes personnalisées (AIRSI sur article)
                            foreach (var tax in customTaxes)
                            {
                                connection.Execute(
                                    @"INSERT INTO CustomTaxes (InvoiceItemId, Name, Amount)
                                    VALUES (@InvoiceItemId, @Name, @Amount)",
                                    new { InvoiceItemId = itemId, Name = tax.Name, Amount = tax.Amount }, transaction: tx
                                );
                            }
                        }

                        // Insertion de la taxe globale si trouvée
                        if (taxeGlobale > 0)
                        {
                            connection.Execute(
                                @"INSERT INTO GlobalCustomTaxes (InvoiceId, Name, Amount)
                          VALUES (@InvoiceId, @Name, @Amount)",
                                new
                                {
                                    InvoiceId = invoiceId,
                                    Name = GetTaxeGlobal(taxeGlobale),
                                    Amount = taxeGlobale
                                },
                                transaction: tx
                            );
                        }

                        //  Mise à jour des totaux
                        connection.Execute(
                            "UPDATE Invoices SET MontantHtTotal=@Ht, MontantTvaTotal=@Tva WHERE Id=@Id",
                            new { Ht = totalHt, Tva = totalTva, Id = invoiceId }, transaction: tx
                        );

                        added++;
                    }

                    tx.Commit();
                }
            }
        }


        public IEnumerable<InvoiceInfo> GetAll()
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();
                connection.Execute("PRAGMA encoding = 'UTF-8';");
                return connection.Query<InvoiceInfo>("SELECT * FROM Invoices ORDER BY ImportDate DESC");
            }
        }

        public IEnumerable<InvoiceItem> GetItems(int invoiceId)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();

                // Récupération des items de la facture
                var items = connection.Query<InvoiceItem>(
                    "SELECT * FROM InvoiceItems WHERE InvoiceId=@Id",
                    new { Id = invoiceId }).ToList();

                // Vérifie si la table CustomTaxes existe
                bool customTaxesExists = connection.ExecuteScalar<int>(
                    "SELECT COUNT(name) FROM sqlite_master WHERE type='table' AND name='CustomTaxes';") > 0;

                if (!customTaxesExists)
                {
                    // Si pas de table CustomTaxes, on retourne les items avec CustomTaxes vide
                    foreach (var item in items)
                        item.CustomTaxes = new List<CustomTaxDraft>();

                    return items;
                }

                // Récupère les taxes pour chaque item
                foreach (var item in items)
                {
                    var taxes = connection.Query<CustomTaxDraft>(
                        "SELECT Name, Amount FROM CustomTaxes WHERE InvoiceItemId=@ItemId",
                        new { ItemId = item.Id }).ToList();

                    item.CustomTaxes = taxes ?? new List<CustomTaxDraft>();
                }

                return items;
            }
        }

        public List<CustomTaxDraft> GetGlobalCustomTaxes(int invoiceId)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();

                bool exists = connection.ExecuteScalar<int>(
                    "SELECT COUNT(name) FROM sqlite_master WHERE type='table' AND name='GlobalCustomTaxes';") > 0;

                if (!exists)
                    return new List<CustomTaxDraft>();

                var taxes = connection.Query<CustomTaxDraft>(
                    "SELECT Name, Amount FROM GlobalCustomTaxes WHERE InvoiceId=@Id",
                    new { Id = invoiceId }).ToList();

                return taxes ?? new List<CustomTaxDraft>();
            }
        }


        public void Delete(int id)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();

                using (var tx = connection.BeginTransaction())
                {
                    try
                    {
                        // Vérifie si la facture existe
                        var invoiceExists = connection.ExecuteScalar<int>(
                            "SELECT COUNT(1) FROM Invoices WHERE Id=@Id",
                            new { Id = id }, transaction: tx
                        ) > 0;

                        if (!invoiceExists)
                        {
                            tx.Rollback();
                            return; // rien à supprimer
                        }

                        // Vérifie si la facture est certifiée
                        bool isCertified = connection.ExecuteScalar<int>(
                            "SELECT IsCertified FROM Invoices WHERE Id=@Id",
                            new { Id = id }, transaction: tx
                        ) == 1;

                        // Supprime la réponse API si certifiée
                        if (isCertified)
                        {
                            //var apiRepo = new ApiResponseRepository("apiResponses.db");
                            var apiRepo = new ApiResponseRepository();
                            apiRepo.DeleteByInvoiceId(id);
                        }

                        // Supprime les lignes si la table existe
                        bool invoiceItemsExists = connection.ExecuteScalar<int>(
                            "SELECT COUNT(name) FROM sqlite_master WHERE type='table' AND name='InvoiceItems';") > 0;

                        if (invoiceItemsExists)
                            connection.Execute(
                                "DELETE FROM InvoiceItems WHERE InvoiceId=@Id",
                                new { Id = id }, transaction: tx
                            );

                        // Supprime les taxes personnalisées si la table existe
                        bool customTaxesExists = connection.ExecuteScalar<int>(
                            "SELECT COUNT(name) FROM sqlite_master WHERE type='table' AND name='CustomTaxes';") > 0;

                        if (customTaxesExists)
                            connection.Execute(
                                @"DELETE FROM CustomTaxes 
                          WHERE InvoiceItemId IN (SELECT Id FROM InvoiceItems WHERE InvoiceId=@Id)",
                                new { Id = id }, transaction: tx
                            );

                        // Supprime l'en-tête de la facture
                        connection.Execute(
                            "DELETE FROM Invoices WHERE Id=@Id",
                            new { Id = id }, transaction: tx
                        );

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw; // remonte l'exception
                    }
                }
            }
        }


        public InvoiceInfo GetById(int id)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();
                return connection.QueryFirstOrDefault<InvoiceInfo>("SELECT * FROM Invoices WHERE Id=@Id", new { Id = id });
            }
        }

        public void MarkAsCertified(int invoiceId)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();
                connection.Execute(
                    "UPDATE Invoices SET IsCertified = 1 WHERE Id = @Id",
                    new { Id = invoiceId }
                );
            }
        }



        /// <summary>
        /// Vérifie si un numéro de facture existe déjà dans la base.
        /// </summary>
        /// <param name="numeroFacture">Numéro à vérifier</param>
        /// <param name="excludeInvoiceId">Optionnel : ID de la facture à exclure (utile pour les mises à jour)</param>
        /// <returns>True si le numéro existe déjà, sinon False</returns>
        public bool InvoiceNumberExists(string numeroFacture, int? excludeInvoiceId = null)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();

                string query = "SELECT COUNT(1) FROM Invoices WHERE NumeroFacture = @NumeroFacture";

                //  Si on met à jour une facture, on exclut son propre ID de la vérification
                if (excludeInvoiceId.HasValue)
                {
                    query += " AND Id <> @ExcludeId";
                }

                int count = connection.ExecuteScalar<int>(
                    query,
                    new { NumeroFacture = numeroFacture, ExcludeId = excludeInvoiceId }
                );

                return count > 0;
            }
        }



        public int InsertInvoice(InvoiceInfo invoice, List<InvoiceItem> items, List<CustomTaxDraft> globalTaxes)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();
                using (var tx = connection.BeginTransaction())
                {
                    try
                    {


                        //  Vérification du numéro de facture
                        if (InvoiceNumberExists(invoice.NumeroFacture))
                        {
                            throw new InvalidOperationException(
                                $"Le numéro de facture '{invoice.NumeroFacture}' existe déjà."
                            );
                        }


                        // 1️⃣ Insert facture principale
                        int invoiceId = connection.ExecuteScalar<int>(
                            @"INSERT INTO Invoices
                        (TypeFacture, NumeroFacture, DateFacture, CompteClient, ReferenceFacture, ParentReference, ImportDate,
                         MontantHtTotal, MontantTvaTotal, Remisetht, IsCertified)
                        VALUES
                        (@TypeFacture, @NumeroFacture, @DateFacture, @CompteClient, @ReferenceFacture, @ParentReference,
                         @ImportDate, @MontantHtTotal, @MontantTvaTotal, @Remisetht, @IsCertified);
                        SELECT last_insert_rowid();",
                            new
                            {
                                invoice.TypeFacture,
                                invoice.NumeroFacture,
                                invoice.DateFacture,
                                invoice.CompteClient,
                                invoice.ReferenceFacture,
                                invoice.ParentReference,
                                ImportDate = invoice.ImportDate.ToString("o"),
                                MontantHtTotal = invoice.MontantHtTotal,
                                MontantTvaTotal = invoice.MontantTvaTotal,
                                Remisetht = invoice.Remisetht,
                                IsCertified = invoice.IsCertified ? 1 : 0,
                            }, transaction: tx
                        );

                        // 2️⃣ Insert des lignes
                        foreach (var item in items)
                        {
                            int itemId = connection.ExecuteScalar<int>(
                                @"INSERT INTO InvoiceItems
                        (InvoiceId, CodeArticle, Libelle, Quantite, Unite, PrixUnitaire, Remise, TypeTaxe, TauxTva)
                        VALUES
                        (@InvoiceId, @CodeArticle, @Libelle, @Quantite, @Unite, @PrixUnitaire, @Remise, @TypeTaxe, @TauxTva);
                        SELECT last_insert_rowid();",
                                new
                                {
                                    InvoiceId = invoiceId,
                                    CodeArticle = item.CodeArticle,
                                    Libelle = item.Libelle,
                                    Quantite = item.Quantite,
                                    Unite = item.Unite,
                                    PrixUnitaire = item.PrixUnitaire,
                                    Remise = item.Remise,
                                    TypeTaxe = item.TypeTaxe,
                                    TauxTva = GetTauxFromTaxe(item.TypeTaxe)
                                }, transaction: tx
                            );

                            if (item.CustomTaxes != null)
                            {
                                foreach (var tax in item.CustomTaxes)
                                {
                                    if (string.IsNullOrWhiteSpace(tax.Name)) continue;
                                    connection.Execute(
                                        @"INSERT INTO CustomTaxes (InvoiceItemId, Name, Amount)
                                  VALUES (@InvoiceItemId, @Name, @Amount);",
                                        new { InvoiceItemId = itemId, tax.Name, tax.Amount }, transaction: tx
                                    );
                                }
                            }
                        }

                        // 3️⃣ Taxes globales
                        if (globalTaxes != null)
                        {
                            foreach (var g in globalTaxes)
                            {
                                if (string.IsNullOrWhiteSpace(g.Name)) continue;
                                connection.Execute(
                                    @"INSERT INTO GlobalCustomTaxes (InvoiceId, Name, Amount)
                              VALUES (@InvoiceId, @Name, @Amount);",
                                    new { InvoiceId = invoiceId, g.Name, g.Amount }, transaction: tx
                                );
                            }
                        }

                        tx.Commit();
                        return invoiceId;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }


        public void UpdateInvoice(InvoiceInfo invoice, List<InvoiceItem> items, List<CustomTaxDraft> globalTaxes)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();

                // Vérification d’unicité (en excluant la facture actuelle)
                if (InvoiceNumberExists(invoice.NumeroFacture, invoice.Id))
                {
                    throw new InvalidOperationException(
                        $"Le numéro de facture '{invoice.NumeroFacture}' existe déjà pour une autre facture."
                    );
                }

                using (var tx = connection.BeginTransaction())
                {
                    try
                    {
                        // 1️⃣ Mise à jour des infos de la facture
                        connection.Execute(
                            @"UPDATE Invoices SET
                        TypeFacture = @TypeFacture,
                        NumeroFacture = @NumeroFacture,
                        DateFacture = @DateFacture,
                        CompteClient = @CompteClient,
                        ReferenceFacture = @ReferenceFacture,
                        ParentReference = @ParentReference,
                        ImportDate = @ImportDate,
                        MontantHtTotal = @MontantHtTotal,
                        MontantTvaTotal = @MontantTvaTotal,
                        Remisetht = @Remisetht,
                        IsCertified = @IsCertified
                      WHERE Id = @Id;",
                            new
                            {
                                Id = invoice.Id,
                                invoice.TypeFacture,
                                invoice.NumeroFacture,
                                invoice.DateFacture,
                                invoice.CompteClient,
                                invoice.ReferenceFacture,
                                invoice.ParentReference,
                                ImportDate = invoice.ImportDate.ToString("o"),
                                MontantHtTotal = invoice.MontantHtTotal,
                                MontantTvaTotal = invoice.MontantTvaTotal,
                                Remisetht = invoice.Remisetht,
                                IsCertified = invoice.IsCertified ? 1 : 0,
                            }, tx
                        );

                        // 2️⃣ Suppression des anciennes lignes et taxes liées
                        connection.Execute("DELETE FROM InvoiceItems WHERE InvoiceId = @InvoiceId;", new { InvoiceId = invoice.Id }, tx);
                        connection.Execute("DELETE FROM CustomTaxes WHERE InvoiceItemId NOT IN (SELECT Id FROM InvoiceItems);", transaction: tx);
                        connection.Execute("DELETE FROM GlobalCustomTaxes WHERE InvoiceId = @InvoiceId;", new { InvoiceId = invoice.Id }, tx);

                        // 3️⃣ Réinsertion des lignes et taxes mises à jour
                        foreach (var item in items)
                        {
                            int itemId = connection.ExecuteScalar<int>(
                                @"INSERT INTO InvoiceItems
                        (InvoiceId, CodeArticle, Libelle, Quantite, Unite, PrixUnitaire, Remise, TypeTaxe, TauxTva)
                        VALUES
                        (@InvoiceId, @CodeArticle, @Libelle, @Quantite, @Unite, @PrixUnitaire, @Remise, @TypeTaxe, @TauxTva);
                        SELECT last_insert_rowid();",
                                new
                                {
                                    InvoiceId = invoice.Id,
                                    CodeArticle = item.CodeArticle,
                                    Libelle = item.Libelle,
                                    Quantite = item.Quantite,
                                    Unite = item.Unite,
                                    PrixUnitaire = item.PrixUnitaire,
                                    Remise = item.Remise,
                                    TypeTaxe = item.TypeTaxe,
                                    TauxTva = GetTauxFromTaxe(item.TypeTaxe)
                                }, tx
                            );

                            if (item.CustomTaxes != null)
                            {
                                foreach (var tax in item.CustomTaxes)
                                {
                                    if (string.IsNullOrWhiteSpace(tax.Name)) continue;
                                    connection.Execute(
                                        @"INSERT INTO CustomTaxes (InvoiceItemId, Name, Amount)
                                  VALUES (@InvoiceItemId, @Name, @Amount);",
                                        new { InvoiceItemId = itemId, tax.Name, tax.Amount }, tx
                                    );
                                }
                            }
                        }

                        // 4️⃣ Réinsertion des taxes globales
                        if (globalTaxes != null)
                        {
                            foreach (var g in globalTaxes)
                            {
                                if (string.IsNullOrWhiteSpace(g.Name)) continue;
                                connection.Execute(
                                    @"INSERT INTO GlobalCustomTaxes (InvoiceId, Name, Amount)
                              VALUES (@InvoiceId, @Name, @Amount);",
                                    new { InvoiceId = invoice.Id, g.Name, g.Amount }, tx
                                );
                            }
                        }

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }


        public List<GlobalCustomTax> GetGlobalCustomTaxesSafe(int invoiceId)
        {
            var rawList = GetGlobalCustomTaxes(invoiceId); // retourne List<CustomTaxDraft>
            var result = new List<GlobalCustomTax>();

            if (rawList == null) return result;

            foreach (var tax in rawList)
            {
                if (tax == null) continue;

                // Création explicite de GlobalCustomTax
                result.Add(new GlobalCustomTax
                {
                    Id = 0,                // pas disponible dans CustomTaxDraft
                    Name = tax.Name,
                    Amount = tax.Amount
                });
            }

            return result;
        }



    }

}


   
