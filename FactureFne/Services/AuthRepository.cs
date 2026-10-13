using System;
using System.Data.SQLite;
using Dapper;
using System.Security.Cryptography;
using System.Text;
using FactureFne.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
namespace FactureFne.Services
{
    public class AuthRepository
    {
        private readonly string _dbPath;

        public AuthRepository(string dbPath = null)
        {
            _dbPath = string.IsNullOrWhiteSpace(dbPath)
                ? DatabaseHelper.GetDatabasePath("Users.db")
                : dbPath;

            InitializeDatabase();
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

                string sqlUsers = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT NOT NULL UNIQUE,
                    PasswordHash TEXT NOT NULL
                );";

                connection.Execute(sqlUsers);

                //  Vérifier si un utilisateur existe déjà
                string checkSql = "SELECT COUNT(1) FROM Users;";
                int count = connection.ExecuteScalar<int>(checkSql);

                if (count == 0)
                {
                    // Aucun utilisateur → on crée un utilisateur par défaut
                    string defaultUser = "Administrateur";
                    string defaultPass = "admin123";

                    string insertSql = "INSERT INTO Users (Username, PasswordHash) VALUES (@Username, @PasswordHash);";
                    connection.Execute(insertSql, new
                    {
                        Username = defaultUser,
                        PasswordHash = HashPassword(defaultPass)
                    });
                }
            }
        
        }

        //  Hachage du mot de passe (SHA256 simple)
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }

        //  Ajout d'un utilisateur
        public bool AddUser(string username, string password)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();

                string sql = "INSERT INTO Users (Username, PasswordHash) VALUES (@Username, @PasswordHash);";

                try
                {
                    connection.Execute(sql, new { Username = username.Trim(), PasswordHash = HashPassword(password) });
                    return true;
                }
                catch
                {
                    // En cas de doublon ou autre erreur
                    return false;
                }
            }
        }

        //  Vérification login
        public bool ValidateUser(string username, string password)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();

                string sql = "SELECT PasswordHash FROM Users WHERE Username = @Username LIMIT 1;";
                string storedHash = connection.ExecuteScalar<string>(sql, new { Username = username.Trim() });

                if (string.IsNullOrEmpty(storedHash))
                    return false;

                return storedHash == HashPassword(password);
            }
        }

        public User GetUserById(int userId)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();

                string sql = "SELECT Id, Username FROM Users WHERE Id = @Id;";
                return connection.QuerySingleOrDefault<User>(sql, new { Id = userId });
            }
        }

        public bool UsernameExists(string username, int? excludeUserId = null)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();

                string sql = excludeUserId == null
                    ? "SELECT COUNT(1) FROM Users WHERE Username = @Username;"
                    : "SELECT COUNT(1) FROM Users WHERE Username = @Username AND Id != @Id;";

                return connection.ExecuteScalar<int>(
                    sql,
                    new { Username = username.Trim(), Id = excludeUserId }
                ) > 0;
            }
        }

        public void UpdateUsername(int userId, string newUsername)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();

                string sql = "UPDATE Users SET Username = @Username WHERE Id = @Id;";
                connection.Execute(sql, new
                {
                    Username = newUsername.Trim(),
                    Id = userId
                });
            }
        }

        public void UpdatePassword(int userId, string newPassword)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();

                string sql = "UPDATE Users SET PasswordHash = @Hash WHERE Id = @Id;";
                connection.Execute(sql, new
                {
                    Hash = HashPassword(newPassword),
                    Id = userId
                });
            }
        }

        public User GetUserByUsername(string username)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();

                string sql = "SELECT Id, Username FROM Users WHERE Username = @Username;";
                return connection.QuerySingleOrDefault<User>(sql, new { Username = username.Trim() });
            }
        }

    }
}
