using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Data.SQLite;
using FactureFne.Models;
using FactureFne.Services;
using FactureFne.Controls;

namespace FactureFne.Services
{
    public class ArticlesRepository
    {
        private readonly string _connectionString;

        public ArticlesRepository()
        {
            _connectionString = DatabaseHelper.GetConnectionString("articles.db");
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS articles (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ref_article TEXT NOT NULL,
                        designation TEXT NOT NULL,
                        quantite REAL DEFAULT 0,
                        unite TEXT,
                        prix REAL
                    );
                ";

                using (var cmd = new SQLiteCommand(createTableQuery, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                // Vérifier si la colonne existe avant d’ajouter
                string checkColumnQuery = "PRAGMA table_info(articles);";
                bool columnExists = false;

                using (var cmd = new SQLiteCommand(checkColumnQuery, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader["name"].ToString().Equals("quantite", StringComparison.OrdinalIgnoreCase))
                        {
                            columnExists = true;
                            break;
                        }
                    }
                }

                // Ajouter la colonne uniquement si elle n’existe pas
                if (!columnExists)
                {
                    string alterQuery = "ALTER TABLE articles ADD COLUMN quantite REAL DEFAULT 0;";
                    using (var cmd = new SQLiteCommand(alterQuery, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }


            }
        }

        public void AddArticle(string reference, string designation, decimal quantite, string unite, decimal prix)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string query = "INSERT INTO articles (ref_article, designation, quantite, unite, prix) VALUES (@ref, @des, @quant, @uni, @prix)";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ref", reference);
                    cmd.Parameters.AddWithValue("@des", designation);
                    cmd.Parameters.AddWithValue("@quant", quantite);
                    cmd.Parameters.AddWithValue("@uni", unite);
                    cmd.Parameters.AddWithValue("@prix", prix);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateArticle(int id, string reference, string designation, decimal quantite, string unite, decimal prix)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string query = "UPDATE articles SET ref_article=@ref, designation=@des, quantite=@quant, unite=@uni, prix=@prix WHERE id=@id";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@ref", reference);
                    cmd.Parameters.AddWithValue("@des", designation);
                    cmd.Parameters.AddWithValue("@quant", quantite);
                    cmd.Parameters.AddWithValue("@uni", unite);
                    cmd.Parameters.AddWithValue("@prix", prix);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteArticle(int id)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string query = "DELETE FROM articles WHERE id=@id";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public DataTable GetAllArticles()
        {
            var dt = new DataTable();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM articles ORDER BY id DESC";
                using (var cmd = new SQLiteCommand(query, conn))
                using (var adapter = new SQLiteDataAdapter(cmd))
                {
                    adapter.Fill(dt);
                }
            }
            return dt;
        }

        public bool ReferenceExists(string reference, int? excludeId = null)
        {
            using (var connection = new SQLiteConnection(DatabaseHelper.GetConnectionString("articles.db")))
            {
                connection.Open();
                string query = "SELECT COUNT(1) FROM articles WHERE ref_article = @ref_article";

                if (excludeId != null)
                    query += " AND id <> @ExcludeId";

                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@ref_article", reference);
                    if (excludeId != null)
                        cmd.Parameters.AddWithValue("@ExcludeId", excludeId);

                    long count = (long)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        //public bool ReferenceExists(string reference, int? excludeId = null)
        //{
        //    using (var connection = new SQLiteConnection(DatabaseHelper.GetConnectionString("articles.db")))
        //    {
        //        connection.Open();
        //        string query = "SELECT COUNT(1) FROM Articles WHERE RefArticle = @RefArticle";

        //        if (excludeId != null)
        //            query += " AND Id <> @ExcludeId";

        //        using (var cmd = new SQLiteCommand(query, connection))
        //        {
        //            cmd.Parameters.AddWithValue("@RefArticle", reference);
        //            if (excludeId != null)
        //                cmd.Parameters.AddWithValue("@ExcludeId", excludeId);

        //            long count = (long)cmd.ExecuteScalar();
        //            return count > 0;
        //        }
        //    }

        //}

        public Article GetByReference(string reference)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM articles WHERE ref_article = @reference";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@reference", reference);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Article
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                Ref_Article = reader["ref_article"].ToString(),
                                Designation = reader["designation"].ToString(),
                                Quantite = Convert.ToDecimal(reader["quantite"]),
                                Unite = reader["unite"].ToString(),
                                Prix = Convert.ToDecimal(reader["prix"])
                            };
                        }
                    }
                }
            }

            return null; // si aucun article trouvé
        }


    }
}
