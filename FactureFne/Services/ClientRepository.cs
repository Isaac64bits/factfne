using Dapper;
using FactureFne.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace FactureFne.Services
{
    public class ClientRepository
    {
        private readonly string _dbPath;

        public static string[] ReadFileAsUtf8(string filePath)
        {
            // Lire avec ISO-8859-1
            var isoEncoding = Encoding.GetEncoding("ISO-8859-1");
            string[] lines;

            using (var reader = new StreamReader(filePath, isoEncoding))
            {
                var content = reader.ReadToEnd();
                // Convertir en UTF-8
                var utf8Bytes = Encoding.Convert(isoEncoding, Encoding.UTF8, isoEncoding.GetBytes(content));
                string utf8Content = Encoding.UTF8.GetString(utf8Bytes);

                // Retourner les lignes en UTF-8
                lines = utf8Content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            }

            return lines;
        }

        public ClientInfo GetByCompteClient(string compteClient)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();
                string sql = "SELECT * FROM Clients WHERE Compte = @Compte LIMIT 1";
                return connection.Query<ClientInfo>(sql, new { Compte = compteClient }).FirstOrDefault();
            }
        }


        private string NormalizeCc(string cc)
        {
            if (string.IsNullOrWhiteSpace(cc))
                return "";

            // Supprimer tous les espaces
            cc = cc.Replace(" ", "").Trim();

            // Mettre la dernière lettre en majuscule si elle existe
            if (cc.Length == 8 && char.IsLetter(cc[7]))
            {
                cc = cc.Substring(0, 7) + char.ToUpper(cc[7]);
            }

            return cc;
        }

        private bool IsValidCc(string cc)
        {
            return Regex.IsMatch(cc, @"^\d{7}[A-Z]$");
        }

        public ClientRepository(string dbPath = null)
        {
            _dbPath = string.IsNullOrWhiteSpace(dbPath)
                ? DatabaseHelper.GetDatabasePath("clients.db")
                : dbPath;

            InitializeDatabase();
        }

        // Méthode de nettoyage
        private string Sanitize(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            string value = input.Trim();

            // Remplacer les apostrophes simples/doubles par des versions typographiques
            value = value.Replace("'", "’").Replace("\"", "”");

            // Supprimer les caractères de contrôle + espaces invisibles
            value = Regex.Replace(value, @"[\x00-\x1F\x7F\u200B-\u200F\u202A-\u202E\uFEFF]", string.Empty);

            // Normalisation Unicode (préserve les accents correctement en UTF-8)
            value = value.Normalize(NormalizationForm.FormC);

            return value;
        }


        public ClientInfo SanitizeClient(ClientInfo client, List<string> errors = null)
        {
            client.Cc = NormalizeCc(client.Cc);
            client.Cc = (client.Cc);
            client.Compte = Sanitize(client.Compte);
            client.Libelle = Sanitize(client.Libelle);
            client.Tel = Sanitize(client.Tel);
            client.Email = Sanitize(client.Email);
            client.CompteComptable = Sanitize(client.CompteComptable);

            //Vérification stricte du CC uniquement si rempli
                     if (!string.IsNullOrWhiteSpace(client.Cc) && !IsValidCc(client.Cc))
            {
                string errorMsg = $"Le NCC '{client.Cc}' est invalide pour le client '{client.Libelle}'. " +
                                  "Format attendu : 7 chiffres + 1 lettre majuscule (ex: 1234567A).";

                // On ajoute dans la liste d'erreurs si fournie
                errors?.Add(errorMsg);

                return null; // rejet du client
            }

            return client;
        }



        private void InitializeDatabase()
        {
                //File.Delete(_dbPath);
            if (!File.Exists(_dbPath))
            {
                SQLiteConnection.CreateFile(_dbPath);
            }
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();
                connection.Execute("PRAGMA encoding = 'UTF-8';");

                string sql = @"
                CREATE TABLE IF NOT EXISTS Clients (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Cc TEXT NOT NULL,
                    Compte TEXT NOT NULL,
                    Libelle TEXT NOT NULL,
                    Tel TEXT,
                    Email TEXT,
                    CompteComptable TEXT,
                    Valeur REAL
                );";
                connection.Execute(sql);
            }

        }

        public IEnumerable<ClientInfo> GetAll()
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {

                connection.Open();
                return connection.Query<ClientInfo>("SELECT * FROM Clients");

            }
        }

        public void Add(ClientInfo client)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();
                connection.Execute("PRAGMA encoding = 'UTF-8';");

                try
                {
                    var cleanClient = SanitizeClient(client);
                    if (cleanClient == null) return; // NCC invalide, on ignore

                    // Vérifie si le client existe déjà
                    string checkSql = "SELECT COUNT(1) FROM Clients WHERE Cc=@Cc AND Compte=@Compte";
                    int count = connection.ExecuteScalar<int>(checkSql, new { cleanClient.Cc, cleanClient.Compte });

                    if (count > 0)
                    {
                        MessageBox.Show($"Le client '{cleanClient.Libelle}' existe déjà dans la base.", "Information",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    string sql = @"
                INSERT INTO Clients (Cc, Compte, Libelle, Tel, Email, CompteComptable, Valeur)
                VALUES (@Cc, @Compte, @Libelle, @Tel, @Email, @CompteComptable, @Valeur);";

                    connection.Execute(sql, cleanClient);

                    MessageBox.Show($"Client '{cleanClient.Libelle}' ajouté avec succès.", "Succès",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'ajout du client : {ex.Message}", "Erreur",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }


        public void Delete(int id)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();
                connection.Execute("DELETE FROM Clients WHERE Id=@Id", new { Id = id });

            }
        }

        public void AddFromTxt(string filePath)
        {
            if (!File.Exists(filePath)) return;

            var lines = ReadFileAsUtf8(filePath);
            var errors = new List<string>();

            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();
                connection.Execute("PRAGMA encoding = 'UTF-8';");

                using (var transaction = connection.BeginTransaction())
                {
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var parts = line.Split(';');
                        if (parts.Length < 7) continue;

                        var client = new ClientInfo
                        {
                            Cc = parts[0].Trim(),
                            Compte = parts[1].Trim(),
                            Libelle = parts[2].Trim(),
                            Tel = parts[3].Trim(),
                            Email = parts[4].Trim(),
                            CompteComptable = parts[5].Trim(),
                            Valeur = decimal.TryParse(parts[6], out var v) ? v : 0
                        };

                        var cleanClient = SanitizeClient(client, errors);
                        if (cleanClient == null) continue; // on skip les clients invalides

                        // Vérifie doublon
                        string checkSql = "SELECT COUNT(1) FROM Clients WHERE Cc=@Cc AND Compte=@Compte";
                        int count = connection.ExecuteScalar<int>(checkSql, new { cleanClient.Cc, cleanClient.Compte }, transaction);
                        if (count > 0) continue;

                        // Insère
                        string sql = @"
                    INSERT INTO Clients (Cc, Compte, Libelle, Tel, Email, CompteComptable, Valeur)
                    VALUES (@Cc, @Compte, @Libelle, @Tel, @Email, @CompteComptable, @Valeur);";
                        connection.Execute(sql, cleanClient, transaction);
                    }

                    transaction.Commit();
                }
            }

            //  Affiche un seul message regroupé
            if (errors.Count > 0)
            {
                MessageBox.Show(
                    "Import terminé avec erreurs :\n\n" + string.Join("\n", errors),
                    "Import terminé",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
            else
            {
                MessageBox.Show("Import terminé avec succès.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }




        public bool Exists(string cc, string compte)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();
                string sql = "SELECT COUNT(1) FROM Clients WHERE Cc=@Cc AND Compte=@Compte";
                int count = connection.ExecuteScalar<int>(sql, new { Cc = cc, Compte = compte });
                return count > 0;
            }
        }

        public void Update(int id, ClientInfo client)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();
                connection.Execute("PRAGMA encoding = 'UTF-8';");

                var cleanClient = SanitizeClient(client);

                string sql = @"
                UPDATE Clients
                SET Cc=@Cc,
                    Compte=@Compte,
                    Libelle=@Libelle,
                    Tel=@Tel,
                    Email=@Email,
                    CompteComptable=@CompteComptable,
                    Valeur=@Valeur
                WHERE Id=@Id;";

                connection.Execute(sql, new
                {
                    Id = id,
                    cleanClient.Cc,
                    cleanClient.Compte,
                    cleanClient.Libelle,
                    cleanClient.Tel,
                    cleanClient.Email,
                    cleanClient.CompteComptable,
                    cleanClient.Valeur
                });
            }
        }


        public ClientInfo GetByCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;

            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();
                // Cherche d'abord sur Compte (identifiant client), puis sur Cc (NCC) — pratique si on ne sait pas lequel on a
                var sql = @"
            SELECT * FROM Clients
            WHERE Compte = @Code
               OR Cc = @Code
            LIMIT 1;";
                return connection.QueryFirstOrDefault<ClientInfo>(sql, new { Code = code });
            }
        }

        public IEnumerable<ClientInfo> SearchByName(string searchTerm, int limit = 50)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<ClientInfo>();

            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();
                string sql = @"SELECT Id, Libelle, Cc, Compte, Tel, Email 
                       FROM Clients 
                       WHERE Libelle LIKE @Search 
                       ORDER BY Libelle 
                       LIMIT @Limit;";
                return connection.Query<ClientInfo>(sql, new
                {
                    Search = searchTerm + "%",
                    Limit = limit
                });
            }
        }

    }
}
