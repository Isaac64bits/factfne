using FactureFne.Models;
using FactureFne.Services;
using System;
//using System.Collections.Generic;
//using System.ComponentModel;
using System.Data;
//using System.Drawing;
using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
using System.IO;
using OfficeOpenXml;
using System.Windows.Forms;

namespace FactureFne.Controls
{
    public partial class ArticlesControl : UserControl
    {
        private readonly ArticlesRepository _repo;
        private readonly AppState _state;
        private int? selectedArticleId = null;

        public ArticlesControl(AppState state)
        {
            InitializeComponent();
            _repo = new ArticlesRepository();
            _state = state;
            dgvArticles.CellDoubleClick += dgvArticles_CellDoubleClick;
            txtRecherche.TextChanged += TxtRecherche_TextChanged;
            LoadArticles();
        }
        private void TxtRecherche_TextChanged(object sender, EventArgs e)
        {
            string keyword = ((TextBox)sender).Text.Trim().ToLower();
            var allArticles = _repo.GetAllArticles();

            var filteredRows = allArticles.AsEnumerable()
                .Where(a =>
                    a.Field<string>("ref_article").ToLower().Contains(keyword) ||
                    a.Field<string>("designation").ToLower().Contains(keyword)
                );

            if (filteredRows.Any())
            {
                dgvArticles.DataSource = filteredRows.CopyToDataTable();
            }
            else
            {
                dgvArticles.DataSource = allArticles.Clone(); // crée un DataTable vide avec la même structure
            }
        }

        private void ShowLoading(string message = "Veuillez patienter...")
        {
            lblLoading.Text = message;
            panelLoading.Visible = true;
            panelLoading.BringToFront();
            Application.DoEvents();
        }

        private void HideLoading()
        {
            panelLoading.Visible = false;
        }


        private void LoadArticles()
        {
            dgvArticles.DataSource = _repo.GetAllArticles();
        }

        private void btn_ajouter_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txt_ArtRef.Text) || string.IsNullOrWhiteSpace(txt_designation.Text))
            {
                MessageBox.Show("Veuillez remplir la référence et la désignation.");
                return;
            }

            if (_repo.ReferenceExists(txt_ArtRef.Text))
            {
                MessageBox.Show("Cette référence existe déjà. Veuillez en choisir une autre.",
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            decimal prix = 0;
            decimal.TryParse(txt_prix.Text, out prix);

            decimal quantite = 0;
            decimal.TryParse(txt_quantite.Text, out quantite);

            _repo.AddArticle(txt_ArtRef.Text, txt_designation.Text, quantite, txt_unite.Text, prix);

            LoadArticles(); // recharge la grille
            ClearFields();
        }

        private void btnRetour_Click(object sender, EventArgs e)
        {
            ((Form1)this.ParentForm).ShowPage(new InvoiceControl(_state));
        }

        private void ClearFields()
        {
            txt_ArtRef.Clear();
            txt_designation.Clear();
            txt_quantite.Clear();
            txt_unite.Clear();
            txt_prix.Clear();
            selectedArticleId = null;
            btn_ajouter.Enabled = true;
            btn_modifier.Enabled = false;
        }

        private void btn_supprimer_Click(object sender, EventArgs e)
        {
            if (dgvArticles.SelectedRows.Count == 0)
            {
                MessageBox.Show(
                    "Veuillez sélectionner au moins un article à supprimer.",
                    "Information",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            DialogResult result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer {dgvArticles.SelectedRows.Count} article(s) sélectionné(s) ?",
                "Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result != DialogResult.Yes)
                return;

            ShowLoading("Suppression en Cours...");
            int supprimés = 0;

            foreach (DataGridViewRow row in dgvArticles.SelectedRows)
            {
                if (row.Cells["id"].Value == null)
                    continue;

                int id = Convert.ToInt32(row.Cells["id"].Value);
                _repo.DeleteArticle(id);
                supprimés++;
            }

            LoadArticles();

            HideLoading();

            MessageBox.Show(
                $"{supprimés} article(s) supprimé(s) avec succès.",
                "Suppression terminée",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void dgvArticles_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = dgvArticles.Rows[e.RowIndex];

                selectedArticleId = Convert.ToInt32(row.Cells["id"].Value);
                txt_ArtRef.Text = row.Cells["ref_article"].Value?.ToString();
                txt_designation.Text = row.Cells["designation"].Value?.ToString();
                txt_quantite.Text = row.Cells["quantite"].Value?.ToString();
                txt_unite.Text = row.Cells["unite"].Value?.ToString();
                txt_prix.Text = row.Cells["prix"].Value?.ToString();

                btn_ajouter.Enabled = false; // désactive l'ajout pour éviter les doublons
                btn_modifier.Enabled = true; // active le bouton modifier
            }
        }

        private void btn_modifier_Click(object sender, EventArgs e)
        {
            if (selectedArticleId == null)
            {
                MessageBox.Show("Veuillez d'abord sélectionner un article à modifier (double-clic).");
                return;
            }

            if (string.IsNullOrWhiteSpace(txt_ArtRef.Text) || string.IsNullOrWhiteSpace(txt_designation.Text))
            {
                MessageBox.Show("Veuillez remplir la référence et la désignation.");
                return;
            }

            if (_repo.ReferenceExists(txt_ArtRef.Text, selectedArticleId))
            {
                MessageBox.Show("Impossible de modifier : cette référence est déjà utilisée par un autre article.",
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            decimal prix = 0;
            decimal.TryParse(txt_prix.Text, out prix);

            decimal quantite = 0;
            decimal.TryParse(txt_quantite.Text, out quantite);

            _repo.UpdateArticle(selectedArticleId.Value, txt_ArtRef.Text, txt_designation.Text, quantite, txt_unite.Text, prix);

            MessageBox.Show("Article mis à jour avec succès !");
            LoadArticles();
            ClearFields();

            // Réinitialiser l’état
            selectedArticleId = null;
            btn_ajouter.Enabled = true;
            btn_modifier.Enabled = false;
        }

        private void btnImporter_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "Importer des articles",
                Filter = "Fichiers Excel (*.xlsx)|*.xlsx|Fichiers texte (*.txt)|*.txt",
                Multiselect = false
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string cheminFichier = ofd.FileName;
                string extension = Path.GetExtension(cheminFichier).ToLowerInvariant();

                switch (extension)
                {
                    case ".xlsx":
                        ShowLoading("Importation en cours...");
                            ImporterDepuisExcel(cheminFichier);
                        HideLoading();
                    break;

                    case ".txt":
                        ShowLoading("Importation en cours...");
                           ImporterDepuisTxt(cheminFichier);
                        HideLoading();
                    break;

                    default:
                        MessageBox.Show(
                            "Format de fichier non supporté",
                            "Erreur",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                        break;
                }
            }

        }

        private void ImporterDepuisTxt(string cheminFichier)
        {
            var lignes = File.ReadAllLines(cheminFichier);

            int lignesImportees = 0;
            int doublons = 0;

            foreach (var ligne in lignes)
            {
                if (string.IsNullOrWhiteSpace(ligne))
                    continue;

                var colonnes = ligne.Split(';');

                if (colonnes.Length != 5)
                    continue;

                string reference = colonnes[0].Trim();
                string designation = colonnes[1].Trim();
                string unite = colonnes[3].Trim();

                if (!decimal.TryParse(colonnes[2], out decimal quantite) ||
                    !decimal.TryParse(colonnes[4], out decimal prix))
                    continue;

                // Doublon détecté
                if (_repo.ReferenceExists(reference))
                {
                    doublons++;
                    continue;
                }

                _repo.AddArticle(reference, designation, quantite, unite, prix);
                lignesImportees++;
            }

            LoadArticles();

            // Message récapitulatif unique (clair et professionnel)
            MessageBox.Show(
                $"Import terminé.\n\n" +
                $"✔ Articles importés : {lignesImportees}\n" +
                $"⚠ Articles ignorés (déjà existants) : {doublons}",
                "Résultat de l'import",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void ImporterDepuisExcel(string cheminFichier)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            int lignesImportees = 0;
            int doublons = 0;

            using (var package = new ExcelPackage(new FileInfo(cheminFichier)))
            {
                var feuille = package.Workbook.Worksheets[0];
                int nbLignes = feuille.Dimension.Rows;

                // On commence à la ligne 2 (ligne 1 = en-têtes)
                for (int i = 2; i <= nbLignes; i++)
                {
                    string reference = feuille.Cells[i, 1].Text.Trim();
                    string designation = feuille.Cells[i, 2].Text.Trim();
                    string unite = feuille.Cells[i, 4].Text.Trim();

                    if (!decimal.TryParse(feuille.Cells[i, 3].Text, out decimal quantite) ||
                        !decimal.TryParse(feuille.Cells[i, 5].Text, out decimal prix))
                        continue;

                    if (string.IsNullOrWhiteSpace(reference))
                        continue;

                    // Doublon
                    if (_repo.ReferenceExists(reference))
                    {
                        doublons++;
                        continue;
                    }

                    _repo.AddArticle(reference, designation, quantite, unite, prix);
                    lignesImportees++;
                }
            }

            LoadArticles();

            MessageBox.Show(
                $"Import Excel terminé.\n\n" +
                $"✔ Articles importés : {lignesImportees}\n" +
                $"⚠ Articles ignorés (déjà existants) : {doublons}",
                "Résultat de l'import",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }


    }
}
