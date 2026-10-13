using System;
using System.IO;
using System.Data.SQLite;

namespace FactureFne.Services
{
    public static class DatabaseHelper
    {
        public const string AppFolderName = "FactureFne";

        // Retourne le chemin vers AppData\FactureFne
        public static string AppDataPath
        {
            get
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string dbFolder = Path.Combine(appData, AppFolderName);
                if (!Directory.Exists(dbFolder))
                    Directory.CreateDirectory(dbFolder); // crée si besoin
                return dbFolder;
            }
        }

        // Retourne le chemin complet vers AppData\FactureFne\<dbFileName>
        public static string GetDatabasePath(string dbFileName)
        {
            if (string.IsNullOrWhiteSpace(dbFileName))
                throw new ArgumentException("dbFileName required", nameof(dbFileName));

            string targetPath = Path.Combine(AppDataPath, dbFileName);

            if (!File.Exists(targetPath))
            {
                // Cherche le modèle dans Program Files\Templates
                string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", dbFileName);

                if (File.Exists(templatePath))
                {
                    // Copie du modèle dans AppData
                    File.Copy(templatePath, targetPath, overwrite: true);
                }
                else
                {
                    // Sinon création d'une DB vide
                    SQLiteConnection.CreateFile(targetPath);
                }
            }

            // S'assurer que le fichier est modifiable
            File.SetAttributes(targetPath, FileAttributes.Normal);

            return targetPath;
        }

        // Initialise toutes les DB au premier lancement
        public static void InitializeDatabases()
        {
            string[] databases = { "clients.db", "apiResponses.db", "invoices.db", "Users.db", "articles.db" };

            foreach (var db in databases)
            {
                GetDatabasePath(db); // gère la copie ou création
            }
        }

        // Retourne la connection string pour une DB
        public static string GetConnectionString(string dbFileName)
        {
            string path = GetDatabasePath(dbFileName);
            return $"Data Source={path};Version=3;";
        }

        // Supprime une DB dans AppData
        public static void DeleteDatabase(string dbFileName)
        {
            string targetPath = Path.Combine(AppDataPath, dbFileName);
            if (File.Exists(targetPath))
                File.Delete(targetPath);
        }

        // Vérifie que la DB est accessible en écriture
        public static bool IsWritable(string dbFileName)
        {
            string path = GetDatabasePath(dbFileName);
            try
            {
                using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.ReadWrite))
                { }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
