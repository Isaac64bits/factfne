using FactureFne.Models;
using FactureFne.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace FactureFne.Controls
{
    public partial class ClientsControl : UserControl
    {
        private readonly AppState _state;
        private readonly ClientRepository _repo;

        public ClientsControl(AppState state)
        {
            InitializeComponent();
            _state = state;

            string dbPath = DatabaseHelper.GetDatabasePath("clients.db");
            _repo = new ClientRepository(dbPath);

            LoadData();
        }

        private void ClientsControl_Load(object sender, EventArgs e)
        {

        }

        private void LoadData()
        {
            dgvClients.DataSource = _repo.GetAll().ToList();

         if (dgvClients.Columns.Contains("libelle"))
                dgvClients.Columns["libelle"].HeaderText = "Nom";
            // Masquer les colonnes techniques
            if (dgvClients.Columns.Contains("Valeur"))
                dgvClients.Columns["Valeur"].Visible = false;

        }


        // ===============================
        // BOUTON AJOUTER / MODIFIER
        // ===============================
        private void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCompte.Text) ||
                string.IsNullOrWhiteSpace(txtLibelle.Text))
            {
                MessageBox.Show("Veuillez remplir au moins Compte et Libellé.",
                    "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var client = new ClientInfo
            {
                Cc = txtCc.Text.Trim(),
                Compte = txtCompte.Text.Trim(),
                Libelle = txtLibelle.Text.Trim(),
                Tel = txtTel.Text.Trim(),
                Email = txtEmail.Text.Trim(),
                CompteComptable = txtCompteComptable.Text.Trim(),
                Valeur = numValeur.Value
            };

            //  Vérification avec SanitizeClient
            var errors = new List<string>();
            var cleanClient = _repo.SanitizeClient(client, errors);

            if (cleanClient == null)
            {
                // Regrouper les erreurs si besoin
                string message = errors.Count > 0
                    ? string.Join("\n", errors)
                    : "Les données saisies sont invalides.";

                MessageBox.Show(message, "Erreur de validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (btnSave.Tag is int idToUpdate)
            {
                // MODE MODIFICATION
                _repo.Update(idToUpdate, cleanClient);
                MessageBox.Show("Client modifié avec succès ",
                    "Modification", MessageBoxButtons.OK, MessageBoxIcon.Information);

                btnSave.Tag = null;
                btnSave.Text = "Ajouter"; // revenir au mode normal
            }
            else
            {
                // MODE AJOUT
                int countBefore = _repo.GetAll().Count(c => c.Cc == cleanClient.Cc && c.Compte == cleanClient.Compte);
                _repo.Add(cleanClient);
                int countAfter = _repo.GetAll().Count(c => c.Cc == cleanClient.Cc && c.Compte == cleanClient.Compte);

                if (countAfter > countBefore)
                    MessageBox.Show("Client ajouté avec succès ", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show("Ce client existe déjà et n'a pas été ajouté.", "Doublon", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            LoadData();
            ClearFields();
        }



        // ===============================
        // BOUTON SUPPRIMER
        // ===============================
        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (dgvClients.SelectedRows.Count == 0)
            {
                MessageBox.Show("Veuillez sélectionner un ou plusieurs clients à supprimer.",
                    "Suppression", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Voulez-vous vraiment supprimer {dgvClients.SelectedRows.Count} client(s) ?",
                "Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            int deleted = 0;
            foreach (DataGridViewRow row in dgvClients.SelectedRows)
            {
                if (row.Cells["Id"].Value != null)
                {
                    int clientId = Convert.ToInt32(row.Cells["Id"].Value);
                    _repo.Delete(clientId);
                    deleted++;
                }
            }

            LoadData();

            MessageBox.Show($"{deleted} client(s) supprimé(s) ",
                "Suppression", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ===============================
        // BOUTON MODIFIER
        // ===============================
        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (dgvClients.SelectedRows.Count != 1)
            {
                MessageBox.Show("Veuillez sélectionner un seul client à modifier.",
                    "Modification", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Récupération du client sélectionné
            var row = dgvClients.SelectedRows[0];
            int clientId = Convert.ToInt32(row.Cells["Id"].Value);

            // Remplir les champs avec les infos du client sélectionné
            txtCc.Text = row.Cells["Cc"].Value?.ToString();
            txtCompte.Text = row.Cells["Compte"].Value?.ToString();
            txtLibelle.Text = row.Cells["Libelle"].Value?.ToString();
            txtTel.Text = row.Cells["Tel"].Value?.ToString();
            txtEmail.Text = row.Cells["Email"].Value?.ToString();
            txtCompteComptable.Text = row.Cells["CompteComptable"].Value?.ToString();
            numValeur.Value = row.Cells["Valeur"].Value != null ? Convert.ToDecimal(row.Cells["Valeur"].Value) : 0;

            // On stocke l'ID dans le Tag du bouton Ajouter pour savoir qu'on modifie
            btnSave.Tag = clientId;
            btnSave.Text = "Enregistrer Modifications";
        }


        // ===============================
        // BOUTON IMPORTER
        // ===============================
        private void btnImportTxt_Click(object sender, EventArgs e)
        {
            using (var openFile = new OpenFileDialog())
            {
                openFile.Filter = "Fichiers texte (*.txt)|*.txt|Tous les fichiers (*.*)|*.*";
                openFile.Title = "Sélectionnez le fichier clients";

                if (openFile.ShowDialog() == DialogResult.OK)
                {
                    int beforeCount = _repo.GetAll().Count();
                    _repo.AddFromTxt(openFile.FileName);
                    int afterCount = _repo.GetAll().Count();

                    int added = afterCount - beforeCount;
                    MessageBox.Show($"{added} client(s) ajouté(s) depuis le fichier .",
                        "Import terminé", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    LoadData();
                }
            }
        }

        // ===============================
        // MÉTHODE POUR NETTOYER LES CHAMPS
        // ===============================
        private void ClearFields()
        {
            txtCc.Clear();
            txtCompte.Clear();
            txtLibelle.Clear();
            txtTel.Clear();
            txtEmail.Clear();
            txtCompteComptable.Clear();
            numValeur.Value = 0;
        }

        private void dgvClients_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                dgvClients.ClearSelection();
                dgvClients.Rows[e.RowIndex].Selected = true; // Sélectionne la ligne cliquée
                btnEdit_Click(sender, EventArgs.Empty);
            }
        }

            // Bouton Rechercher
        private void btnSearchClient_Click(object sender, EventArgs e)
        {
            string searchCompte = txtSearchCompte.Text.Trim();

            if (string.IsNullOrWhiteSpace(searchCompte))
            {
                MessageBox.Show("Veuillez entrer le numéro de compte du Client.", "Recherche",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool found = false;

            foreach (DataGridViewRow row in dgvClients.Rows)
            {
                if (row.Cells["Compte"].Value != null &&
                    row.Cells["Compte"].Value.ToString().Equals(searchCompte, StringComparison.OrdinalIgnoreCase))
                {
                    //  Sélection de la ligne trouvée
                    row.Selected = true;
                    dgvClients.FirstDisplayedScrollingRowIndex = row.Index; // scroll automatique
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                MessageBox.Show($"Aucun client trouvé avec le compte '{searchCompte}'.",
                    "Résultat", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            ((Form1)this.ParentForm).ShowPage(new InvoiceControl(_state));
        }
    }
}
