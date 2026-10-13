using FactureFne.Models;
using FactureFne.Services;
using System.Data;
using System;
using Dapper;
using System.Data.SQLite;
using System.Linq;
using System.Windows.Forms;

namespace FactureFne.Controls
{
    public partial class CertifiedInvoicesControl : UserControl
    {
        private readonly ApiResponseRepository _apiRepo;
        private readonly AppState _state;


        // Constructeur sans paramètre : nécessaire pour le Designer WinForms
        public CertifiedInvoicesControl(AppState state)
        {
            InitializeComponent();
            _state = state;
            _apiRepo = new ApiResponseRepository(DatabaseHelper.GetDatabasePath("apiResponses.db"));

            dgvApiResponses.CellMouseDown += dataGridView1_CellMouseDown;
            LoadApiResponses();
        }


        private void LoadApiResponses()
        {
            var apiResponses = _apiRepo.GetAll().ToList();
            var invoiceRepo = new InvoiceRepository(DatabaseHelper.GetDatabasePath("invoices.db"));

            // Créez une liste enrichie avec le numéro de facture
            var displayList = apiResponses.Select(apiResp =>
            {
                var invoice = invoiceRepo.GetById(apiResp.InvoiceId);
                return new
                {
                    apiResp.Id,
                    apiResp.InvoiceId,
                    NumeroFacture = invoice?.NumeroFacture ?? "", // ou invoice?.NumeroFacture selon votre modèle
                    apiResp.ApiReference,
                    apiResp.ApiToken,
                    apiResp.ApiStatus,
                    apiResp.ItemsJson,
                    apiResp.RawResponse,
                    apiResp.CreatedAt
                };
            }).ToList();

            dgvApiResponses.AutoGenerateColumns = true;
            dgvApiResponses.DataSource = displayList;

            // Vous pouvez maintenant afficher la colonne "NumeroFacture"
            if (dgvApiResponses.Columns.Contains("NumeroFacture"))
                dgvApiResponses.Columns["NumeroFacture"].HeaderText = "Numéro Facture";


            // Convertir ApiToken en colonne cliquable
            if (dgvApiResponses.Columns.Contains("ApiToken"))
            {
                var linkCol = new DataGridViewLinkColumn();
                linkCol.DataPropertyName = "ApiToken";
                linkCol.Name = "ApiToken";
                linkCol.HeaderText = "Lien de vérification";
                linkCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

                // Remplacer la colonne texte par une colonne lien
                int index = dgvApiResponses.Columns["ApiToken"].Index;
                dgvApiResponses.Columns.Remove("ApiToken");
                dgvApiResponses.Columns.Insert(index, linkCol);
            }


                // Attacher l’événement clic
            //dgvApiResponses.CellContentClick += dgvApiResponses_CellDoubleClick;


            // Masquer les colonnes techniques
            if (dgvApiResponses.Columns.Contains("Id"))
                dgvApiResponses.Columns["Id"].Visible = true;

            if (dgvApiResponses.Columns.Contains("InvoiceId"))
                dgvApiResponses.Columns["InvoiceId"].Visible = false;
            //dgvApiResponses.Columns["InvoiceId"].HeaderText = "Facture ID";

            if (dgvApiResponses.Columns.Contains("ApiReference"))
                dgvApiResponses.Columns["ApiReference"].HeaderText = "Référence API";

            if (dgvApiResponses.Columns.Contains("ApiToken"))
                dgvApiResponses.Columns["ApiToken"].HeaderText = "Lien vérification";

            if (dgvApiResponses.Columns.Contains("ApiStatus"))
                dgvApiResponses.Columns["ApiStatus"].HeaderText = "Statut";

            if (dgvApiResponses.Columns.Contains("ItemsJson"))
            {
                dgvApiResponses.Columns["ItemsJson"].HeaderText = "ItemsJson";
                dgvApiResponses.Columns["ItemsJson"].Visible = false; // format court
            }

            if (dgvApiResponses.Columns.Contains("CreatedAt"))
            {
                dgvApiResponses.Columns["CreatedAt"].HeaderText = "Date Réception";
            }

            if (dgvApiResponses.Columns.Contains("RawResponse"))
            {
                dgvApiResponses.Columns["RawResponse"].HeaderText = "Réponse JSON";
                dgvApiResponses.Columns["RawResponse"].Visible = false; // caché par défaut
            }

            dgvApiResponses.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;

        }

        private void dataGridView1_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                dgvApiResponses.ClearSelection();
                dgvApiResponses[e.ColumnIndex, e.RowIndex].Selected = true;

                ContextMenuStrip menu = new ContextMenuStrip();
                menu.Items.Add("Copier", null, (s, ev) =>
                {
                    Clipboard.SetText(dgvApiResponses[e.ColumnIndex, e.RowIndex].Value?.ToString());
                });

                menu.Show(Cursor.Position);
            }
        }


        private void dgvApiResponses_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) // pour ignorer l'entête
            {
                var row = dgvApiResponses.Rows[e.RowIndex];
                string url = row.Cells["ApiToken"].Value?.ToString();

                if (!string.IsNullOrWhiteSpace(url))
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = url,
                            UseShellExecute = true  // important pour ouvrir dans le navigateur
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Impossible d’ouvrir le lien : " + ex.Message,
                            "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Aucun lien de vérification trouvé pour cette facture.",
                        "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void dgvApiResponses_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void CertifiedInvoicesControl_Load(object sender, EventArgs e)
        {

        }

        private void btnFilter_Click(object sender, EventArgs e)
        {
            DateTime startLocal = dtpStart.Value.Date;
            DateTime endLocal = dtpEnd.Value.Date.AddDays(1).AddTicks(-1);

            DateTime startUtc = DateTime.SpecifyKind(startLocal, DateTimeKind.Local).ToUniversalTime();
            DateTime endUtc = DateTime.SpecifyKind(endLocal, DateTimeKind.Local).ToUniversalTime();

            var result = _apiRepo.GetByDateRange(startUtc, endUtc).ToList();

            dgvApiResponses.DataSource = result;

            if (dgvApiResponses.Columns.Contains("CreatedAt"))
            {
                dgvApiResponses.Columns["CreatedAt"].HeaderText = "Dates Réception";
                dgvApiResponses.Columns["CreatedAt"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            ((Form1)this.ParentForm).ShowPage(new InvoiceControl(_state));
        }
    }
}
