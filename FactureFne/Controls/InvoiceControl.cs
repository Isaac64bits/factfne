using FactureFne.Models;
using FactureFne.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Printing;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using DrawingFont = System.Drawing.Font;
using System.Text.RegularExpressions;



namespace FactureFne.Controls
{
    public partial class InvoiceControl : UserControl
    {
        private ApiResponseRepository _apiRepo;
        private readonly AppState _state;
        private readonly InvoiceRepository _repo;
        private int? _selectedInvoiceId = null; // pour stocker l'ID de la facture sélectionnée
        private readonly ClientRepository _clientRepo;


        public InvoiceControl(AppState state)
        {
            InitializeComponent();
            this.Font = new DrawingFont("Segoe UI", 9, FontStyle.Regular, GraphicsUnit.Point);
            _state = state;
            string apiDbPath = DatabaseHelper.GetDatabasePath("apiResponses.db");
            string invoiceDbPath = DatabaseHelper.GetDatabasePath("invoices.db");

            _apiRepo = new ApiResponseRepository(apiDbPath);
            _repo = new InvoiceRepository(invoiceDbPath);
            _clientRepo = new ClientRepository(DatabaseHelper.GetDatabasePath("clients.db"));

            LoadInvoices();
            // Remplissage des combos
            cboInvoiceType.Items.AddRange(new[] { "sale", "purchase" });
            cboInvoiceType.SelectedItem = _state.Invoice.InvoiceType;  // valeur par défaut

            cboPaymentMethod.Items.AddRange(new[] { "cash", "card", "check", "mobile-money", "transfer", "deferred" });
            cboPaymentMethod.SelectedItem = _state.Invoice.PaymentMethod;  // valeur par défaut

            cboTemplate.Items.AddRange(new[] { "B2B", "B2C", "B2F", "B2G" });
            cboTemplate.SelectedItem = _state.Invoice.Template;  // valeur par défaut

            // Checkbox + champ Rne
            chkIsRne.Checked = _state.Invoice.IsRne;
            txtRne.Text = _state.Invoice.Rne;
            txtRne.Enabled = chkIsRne.Checked; // actif uniquement si true

            // Événement quand on sélectionne une facture dans le DataGridView
            dgvInvoices.SelectionChanged += Dgv_Invoices_SelectionChanged;

            // Événement du bouton Imprimer
            btn_PrintInvoice.Click += Btn_PrintInvoice_Click;

            //Evenement permetant de copier l'élément d'une cellule
            dgvInvoices.CellMouseDown += dataGridView1_CellMouseDown;


            ToolTip tip = new ToolTip();

            // Configuration optionnelle
            tip.AutoPopDelay = 5000; // durée d'affichage (5 sec)
            tip.InitialDelay = 500;  // délai avant d'apparition
            tip.ReshowDelay = 200;   // délai entre deux apparitions
            tip.ShowAlways = true;   // toujours afficher même si la fenêtre n'est pas active

            // Liaison du texte d'aide
            tip.SetToolTip(btn_PrintInvoice, "Cliquez ici pour générer la facture au format PDF");
            //tip.SetToolTip(txtNomClient, "Entrez le nom complet du client");
        }

        private void dataGridView1_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                dgvInvoices.ClearSelection();
                dgvInvoices[e.ColumnIndex, e.RowIndex].Selected = true;

                ContextMenuStrip menu = new ContextMenuStrip();
                menu.Items.Add("Copier", null, (s, ev) =>
                {
                    Clipboard.SetText(dgvInvoices[e.ColumnIndex, e.RowIndex].Value?.ToString());
                });

                menu.Show(Cursor.Position);
            }
        }


        private void chkIsRne_CheckedChanged(object sender, EventArgs e)
        {
            txtRne.Enabled = chkIsRne.Checked;

            if (!chkIsRne.Checked)
            {
                txtRne.Text = "";
            }
            else
            {
                if (string.IsNullOrWhiteSpace(txtRne.Text))
                {
                    MessageBox.Show("Le Numero de reçu doit être renseigné si vous cochez la case.",
                        "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    // On force le focus dans le champ
                    txtRne.Focus();
                }
            }
        }

        public void LoadInvoices()
        {
            dgvInvoices.AutoGenerateColumns = true;
            dgvInvoices.DataSource = _repo.GetAll().ToList();
            dgvItems.DataSource = null;

            // Masquer les colonnes techniques
            if (dgvInvoices.Columns.Contains("Id"))
                dgvInvoices.Columns["Id"].Visible = false;

            // Renommer les colonnes pour l’affichage
            if (dgvInvoices.Columns.Contains("TypeFacture"))
                dgvInvoices.Columns["TypeFacture"].HeaderText = "Type Facture"; // 6 = Vente, 7 = Avoir

            if (dgvInvoices.Columns.Contains("NumeroFacture"))
                dgvInvoices.Columns["NumeroFacture"].HeaderText = "NuméroFacture";

            if (dgvInvoices.Columns.Contains("DateFacture"))
                dgvInvoices.Columns["DateFacture"].HeaderText = "DateFacture";

            if (dgvItems.Columns.Contains("ReferenceFacture"))
                dgvItems.Columns["ReferenceFacture"].HeaderText = "Réf Facture";

            if (dgvInvoices.Columns.Contains("CompteClient"))
                dgvInvoices.Columns["CompteClient"].HeaderText = "Compte Client";

            if (dgvInvoices.Columns.Contains("ImportDate"))
            {
                dgvInvoices.Columns["ImportDate"].HeaderText = "Date Import";
                dgvInvoices.Columns["ImportDate"].DefaultCellStyle.Format = "g";
            }

            if (dgvInvoices.Columns.Contains("MontantHtTotal"))
            {
                dgvInvoices.Columns["MontantHtTotal"].HeaderText = "Montant HT";
                dgvInvoices.Columns["MontantHtTotal"].DefaultCellStyle.Format = "N2";
            }

            if (dgvInvoices.Columns.Contains("MontantTvaTotal"))
            {
                dgvInvoices.Columns["MontantTvaTotal"].HeaderText = "Montant TVA";
                dgvInvoices.Columns["MontantTvaTotal"].DefaultCellStyle.Format = "N2";
            }

            if (dgvInvoices.Columns.Contains("Remisetht"))
            {
                dgvInvoices.Columns["Remisetht"].HeaderText = "Remise TOTAL(%)";
                dgvInvoices.Columns["Remisetht"].DefaultCellStyle.Format = "N2";
            }

            if (dgvInvoices.Columns.Contains("IsCertified"))
                dgvInvoices.Columns["IsCertified"].HeaderText = "Certifiée ?";
        }


        private void btnImport_Click(object sender, EventArgs e)
        {
            using (var openFile = new OpenFileDialog())
            {
                openFile.Filter = "Fichiers texte (*.txt)|*.txt|Tous les fichiers (*.*)|*.*";
                openFile.Title = "Importer un fichier de factures";

                if (openFile.ShowDialog() == DialogResult.OK)
                {
                    int added, duplicates;
                    _repo.ImportFromTxt(openFile.FileName, out added, out duplicates, out int qte);
                    LoadInvoices();

                    MessageBox.Show($"{added} facture(s) ajoutée(s).\n{duplicates} doublon(s) ignoré(s).",
                        "Import terminé", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    if (qte > 0) 
                    {
                    MessageBox.Show($"{qte} ligne(s) facture(s) Avec une quantité <= 0 ont été Ignorée(s).",
                            "Import terminé", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (dgvInvoices.SelectedRows.Count == 0)
            {
                MessageBox.Show("Veuillez sélectionner une ou plusieurs factures à supprimer.",
                    "Suppression", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            foreach (DataGridViewRow row in dgvInvoices.SelectedRows)
            {
                if (row.Cells["Id"].Value != null)
                {
                    int id = Convert.ToInt32(row.Cells["Id"].Value);
                    var invoice = _repo.GetById(id);

                    // Vérification si la facture est certifiée
                    if (invoice.IsCertified)
                    {
                        MessageBox.Show($"La facture {invoice.NumeroFacture} est certifiée et ne peut pas être supprimée.",
                            "Suppression impossible", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }

            if (MessageBox.Show("Supprimer les factures sélectionnées ?",
                "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                foreach (DataGridViewRow row in dgvInvoices.SelectedRows)
                {
                    if (row.Cells["Id"].Value != null)
                    {
                        int id = Convert.ToInt32(row.Cells["Id"].Value);
                        _repo.Delete(id);
                    }
                }
                LoadInvoices();

                MessageBox.Show("Suppression effectuée avec succès !",
                   "Suppression", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void dgvInvoices_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvInvoices.SelectedRows.Count == 1)
            {
                int id = Convert.ToInt32(dgvInvoices.SelectedRows[0].Cells["Id"].Value);
                var items = _repo.GetItems(id).ToList();
                dgvItems.DataSource = items;
                dgvItems.AutoGenerateColumns = true;

                // Masquer les colonnes techniques
                if (dgvItems.Columns.Contains("Id"))
                    dgvItems.Columns["Id"].Visible = false;

                if (dgvItems.Columns.Contains("InvoiceId"))
                    dgvItems.Columns["InvoiceId"].Visible = false;

                // Renommer pour affichage
                if (dgvItems.Columns.Contains("CodeArticle"))
                    dgvItems.Columns["CodeArticle"].HeaderText = "Ref Article";

                if (dgvItems.Columns.Contains("Libelle"))
                    dgvItems.Columns["Libelle"].HeaderText = "Désignation";

                if (dgvItems.Columns.Contains("Quantite"))
                {
                    dgvItems.Columns["Quantite"].HeaderText = "Quantité";
                    dgvItems.Columns["Quantite"].DefaultCellStyle.Format = "N4";
                }

                if (dgvItems.Columns.Contains("Unite"))
                    dgvItems.Columns["Unite"].HeaderText = "Unité";

                if (dgvItems.Columns.Contains("PrixUnitaire"))
                {
                    dgvItems.Columns["PrixUnitaire"].HeaderText = "Prix Unitaire";
                    dgvItems.Columns["PrixUnitaire"].DefaultCellStyle.Format = "N2";
                }

                if (dgvItems.Columns.Contains("Remise"))
                {
                    dgvItems.Columns["Remise"].HeaderText = "Remise (%)";
                    dgvItems.Columns["Remise"].DefaultCellStyle.Format = "N2";
                }

                if (dgvItems.Columns.Contains("Taxe"))
                    dgvItems.Columns["Taxe"].HeaderText = "Type Taxe"; // ex : TVA, TVAB…

                if (dgvItems.Columns.Contains("TauxTva"))
                {
                    dgvItems.Columns["TauxTva"].HeaderText = "TVA (%)";
                    dgvItems.Columns["TauxTva"].DefaultCellStyle.Format = "N2";
                }

                
            }
            else
            {
                dgvItems.DataSource = null;
            }
        }

        private bool ValidateInvoice(string template, ClientInfo client, CompanyInfo company)
        {
            if (client == null)
            {
                MessageBox.Show("Le client est introuvable.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            string normalizedTemplate = template?.Trim().ToUpperInvariant() ?? string.Empty;

            // Le NCC est obligatoire et doit être valide pour B2B et B2G.
            if (normalizedTemplate == "B2B" || normalizedTemplate == "B2G")
            {
                if (string.IsNullOrWhiteSpace(client.Cc))
                {
                    MessageBox.Show($"Template {normalizedTemplate} : le numéro de compte contribuable est obligatoire.",
                        "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                if (!Regex.IsMatch(client.Cc, @"\A\d{7}[A-Z]\z", RegexOptions.CultureInvariant))
                {
                    MessageBox.Show(
                        "Le numéro de compte contribuable doit contenir exactement 8 caractères : " +
                        "7 chiffres suivis d'une lettre majuscule, sans espace (ex. : 1234567A).",
                        "Numéro de compte contribuable invalide",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return false;
                }
            }

            switch (normalizedTemplate)
            {
                case "B2B":
                    if (string.IsNullOrWhiteSpace(client.Email))
                    {
                        MessageBox.Show("Template B2B : l’adresse électronique du client est obligatoire.",
                            "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(client.Tel))
                    {
                        MessageBox.Show("Template B2B : le numéro de téléphone du client est obligatoire.",
                            "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                    break;

                case "B2C":
                case "B2G":
                case "B2F":
                    if (string.IsNullOrWhiteSpace(client.Libelle))
                    {
                        MessageBox.Show($"Template {normalizedTemplate} : nom du client requis.", "Validation",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(client.Tel))
                    {
                        MessageBox.Show($"Template {normalizedTemplate} : numéro de téléphone du client obligatoire.",
                            "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(client.Email))
                    {
                        MessageBox.Show($"Template {normalizedTemplate} : adresse électronique du client obligatoire.",
                            "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }

                    if (normalizedTemplate == "B2F")
                    {
                        if (company == null || string.IsNullOrWhiteSpace(company.ForeignCurrency))
                        {
                            MessageBox.Show("Template B2F : la devise du pays de destination est obligatoire.",
                                "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return false;
                        }

                        if (company.ForeignCurrencyRate <= 0)
                        {
                            MessageBox.Show("Template B2F : le taux de change du pays de destination doit être supérieur à zéro.",
                                "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return false;
                        }
                    }
                    break;
            }

            if (!ValidatePhoneFormat(normalizedTemplate, client.Tel))
                return false;

            if (chkIsRne.Checked && string.IsNullOrWhiteSpace(txtRne.Text))
            {
                MessageBox.Show("Le numéro de reçu est obligatoire si la case est cochée.",
                    "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRne.Focus();
                return false;
            }

            return true;
        }


        private async Task<string> SendInvoiceAsync(AppState state, InvoiceInfo invoice, List<InvoiceItem> items, ClientInfo client)
        {
            //  Construire le JSON
            string json = FneJsonBuilder.BuildInvoiceJson(state, invoice, items, client);
            txtJsonPreview.Text = json;

            //  Appel API
            var api = new FneApiService(state.Api.BaseUrl, state.Api.Token, state.Api.TimeoutSeconds);
            return await api.SendInvoiceAsync(json);
        }

        private async void btnSend_Click(object sender, EventArgs e)
        {
            if (dgvInvoices.SelectedRows.Count == 0)
            {
                MessageBox.Show("Veuillez sélectionner une facture à envoyer.");
                return;
            }

            UpdateInvoiceFromUI();

            int id = Convert.ToInt32(dgvInvoices.SelectedRows[0].Cells["Id"].Value);
            var invoice = _repo.GetById(id);
            var items = _repo.GetItems(id).ToList();

            try
            {
                // Vérifier si la facture est déjà certifiée
                if (invoice.IsCertified)
                {
                    MessageBox.Show("Cette facture est déjà certifiée et ne peut pas être renvoyée.",
                        "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Charger le client (utile pour ventes)
                var clientRepo = new ClientRepository(DatabaseHelper.GetDatabasePath("clients.db"));
                var client = clientRepo.GetByCode(invoice.CompteClient);
                if (client == null)
                {
                    MessageBox.Show("Client introuvable pour le compte : " + invoice.CompteClient,
                        "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Selon le type de facture (TypeFacture provient de l'import : "VENTE" ou "AVOIR")
                if (string.Equals(invoice.TypeFacture, "VENTE", StringComparison.OrdinalIgnoreCase))
                {
                    await SendSaleAsync(invoice, items, client);
                }
                else if (string.Equals(invoice.TypeFacture, "AVOIR", StringComparison.OrdinalIgnoreCase))
                {
                    await SendRefundAsync(invoice, items);
                }
                else
                {
                    MessageBox.Show("Type de facture non reconnu : " + invoice.TypeFacture,
                        "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                txtJsonPreview.Text = ex.Message;
                MessageBox.Show("Erreur lors de l’envoi : " + ex.Message,
                   "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task SendSaleAsync(InvoiceInfo invoice, List<InvoiceItem> items, ClientInfo client)
        {
            // Validation B2B/B2C/B2F/B2G
            if (!ValidateInvoice(_state.Invoice.Template, client, _state.Company))
                return;

            //  Charger les taxes particulières globales avant l'envoi
            var invoiceRepo = new InvoiceRepository(DatabaseHelper.GetDatabasePath("invoices.db"));
            invoice.GlobalCustomTaxes = invoiceRepo.GetGlobalCustomTaxes(invoice.Id);


            // Construire et envoyer
            string response = await SendInvoiceAsync(_state, invoice, items, client);
            var apiResponseObj = JsonConvert.DeserializeObject<ApiInvoiceResponse>(response);

            if (apiResponseObj?.Invoice == null)
            {
                MessageBox.Show("Réponse API invalide : " + response,
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Afficher résumé
            MessageBox.Show(
                $"Facture envoyée avec succès !\n" +
                $"Référence : {apiResponseObj.Reference ?? apiResponseObj.Invoice.Reference}\n" +
                $"Statut : {apiResponseObj.Invoice.Status}\n" +
                $"Montant : {apiResponseObj.Invoice.Amount}\n" +
                $"Token : {apiResponseObj.Token}",
                "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information
            );

            txtJsonPreview.Text = response;

            // Construire le mapping Local API
            var itemsMapping = new List<ApiItemMapping>();
            if (apiResponseObj.Invoice.Items != null && apiResponseObj.Invoice.Items.Any())
            {
                foreach (var apiItem in apiResponseObj.Invoice.Items)
                {
                    // Trouver la ligne locale correspondante (par référence article ou index)
                    var localItem = items.FirstOrDefault(i =>
                        string.Equals(i.CodeArticle?.Trim(), apiItem.Reference?.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (localItem == null)
                        continue;

                    itemsMapping.Add(new ApiItemMapping
                    {
                        LocalItemId = localItem.Id,
                        ApiItemId = apiItem.Id,          // ID unique généré par l’API
                        Reference = apiItem.Reference,   // Ref article côté API
                        Quantity = apiItem.Quantity
                    });
                }
            }

            // Sauvegarde de la réponse + mapping
            _apiRepo.Save(new ApiResponse
            {
                InvoiceId = invoice.Id,
                ApiReference = apiResponseObj.Reference ?? apiResponseObj.Invoice.Reference,
                ApiToken = apiResponseObj.Token,
                ApiStatus = apiResponseObj.Invoice.Status,
                ApiInvoiceId = apiResponseObj.Invoice?.Id ?? "",
                ItemsJson = JsonConvert.SerializeObject(itemsMapping), //  mapping stocké
                RawResponse = response,
                CreatedAt = DateTime.UtcNow
            });

            //  Marquer comme certifiée si statut payé
            if (string.Equals(apiResponseObj.Invoice.Status, "paid", StringComparison.OrdinalIgnoreCase))
            {
                _repo.MarkAsCertified(invoice.Id);
                LoadInvoices();
            }
        }

        private async Task SendRefundAsync(InvoiceInfo refundInvoice, List<InvoiceItem> refundItemsLocal)
        {
            var originalApiResponse = _apiRepo.GetByPartialReference(refundInvoice.ParentReference ?? "");
            if (originalApiResponse == null || string.IsNullOrWhiteSpace(originalApiResponse.ApiInvoiceId))
            {
                MessageBox.Show("Impossible de trouver la facture d’origine (réponse API) pour cet avoir. Vérifiez la référence d'origine.",
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var mapping = originalApiResponse.GetItemsMapping();
            var refundPayloadItems = new List<RefundItem>();

            foreach (var local in refundItemsLocal)
            {
                var map = mapping
                    .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m.Reference) &&
                                         string.Equals(m.Reference.Trim(), local.CodeArticle?.Trim(), StringComparison.OrdinalIgnoreCase))
                    ?? mapping.FirstOrDefault(m => m.LocalItemId == local.Id)
                    ?? mapping.FirstOrDefault(m => m.Reference != null &&
                                                   m.Reference.IndexOf(local.CodeArticle.Trim(), StringComparison.OrdinalIgnoreCase) >= 0);

                if (map == null || string.IsNullOrWhiteSpace(map.ApiItemId))
                {
                    MessageBox.Show($"Impossible de trouver l'ID API de l'article local '{local.CodeArticle}'. Annulation de l'envoi de l'avoir.",
                        "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Forcer quantité positive entière ≥ 1
                int qty = (int)Math.Ceiling(Math.Abs(local.Quantite));
                if (qty <= 0)
                {
                    MessageBox.Show($"Quantité invalide pour l'article {local.CodeArticle} : {local.Quantite}",
                        "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue; // ignore cette ligne
                }

                // Debug
                txtJsonPreview.Text += $"Mapping: {local.CodeArticle} -> {map.ApiItemId}, Quantité: {qty}\r\n";

                refundPayloadItems.Add(new RefundItem
                {
                    Id = map.ApiItemId,
                    Quantity = qty
                });
            }

            if (!refundPayloadItems.Any())
            {
                MessageBox.Show("Aucun article valide pour le remboursement.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var api = new FneApiService(_state.Api.BaseUrl, _state.Api.Token, _state.Api.TimeoutSeconds);
            string result = await api.SendRefundAsync(originalApiResponse.ApiInvoiceId, refundPayloadItems);

            //  Désérialiser la réponse JSON d’un avoir
            var refundResponse = JsonConvert.DeserializeObject<ApiRefundResponse>(result);

            if (refundResponse == null)
            {
                MessageBox.Show("Réponse API invalide pour l’avoir : " + result,
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            //  Affichage résumé
            MessageBox.Show(
                $"Avoir certifié avec succès !\n" +
                $"Référence : {refundResponse.Reference}\n" +
                $"NCC : {refundResponse.Ncc}\n" +
                $"Token : {refundResponse.Token}\n" +
                $"Sticker Restant : {refundResponse.BalanceSticker}",
                "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information
            );

            //  Mettre le JSON brut dans la zone debug
            txtJsonPreview.Text += "\r\n" + result;

            //  Sauvegarder les infos de l’avoir
            _apiRepo.Save(new ApiResponse
            {
                InvoiceId = refundInvoice.Id,
                ApiReference = refundResponse.Reference, // nouvelle référence d’avoir
                ApiToken = refundResponse.Token,
                ApiStatus = "refund",
                ApiInvoiceId = originalApiResponse.ApiInvoiceId, // conserve le lien avec la facture d’origine
                ItemsJson = JsonConvert.SerializeObject(refundPayloadItems),
                RawResponse = result,
                CreatedAt = DateTime.UtcNow
            });

            //  Marquer la facture d’avoir comme certifiée
            _repo.MarkAsCertified(refundInvoice.Id);

            //  Rafraîchir la grille
            LoadInvoices();

        }

        private void UpdateInvoiceFromUI()
        {
            _state.Invoice.InvoiceType = cboInvoiceType.SelectedItem?.ToString() ?? "sale";
            _state.Invoice.PaymentMethod = cboPaymentMethod.SelectedItem?.ToString() ?? "cash";
            _state.Invoice.Template = cboTemplate.SelectedItem?.ToString() ?? "B2B";
            _state.Invoice.IsRne = chkIsRne.Checked;
            _state.Invoice.Rne = txtRne.Text.Trim();
        }



        private void btnSearchFacture_Click(object sender, EventArgs e)
        {
            string searchFacture = txtSearchFacture.Text.Trim();

            if (string.IsNullOrWhiteSpace(searchFacture))
            {
                MessageBox.Show("Veuillez entrer un numéro de facture.", "Recherche",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool found = false;

            foreach (DataGridViewRow row in dgvInvoices.Rows)
            {
                if (row.Cells["NumeroFacture"].Value != null &&
                    row.Cells["NumeroFacture"].Value.ToString().Equals(searchFacture, StringComparison.OrdinalIgnoreCase))
                {
                    //  Sélection de la ligne trouvée
                    row.Selected = true;
                    dgvInvoices.FirstDisplayedScrollingRowIndex = row.Index; // scroll automatique
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                MessageBox.Show($"Aucune facture trouvée avec le numéro '{searchFacture}'.",
                    "Résultat", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }


        private void btn_Modifier_Click(object sender, EventArgs e)
        {
            if (dgvInvoices.CurrentRow == null)
            {
                MessageBox.Show("Veuillez sélectionner une facture à modifier.", 
                    "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Conversion explicite en int puisque la base de données utilise int pour les IDs
            if (!int.TryParse(dgvInvoices.CurrentRow.Cells["Id"].Value.ToString(), out int invoiceId))
            {
                MessageBox.Show("Impossible de récupérer la facture sélectionnée.", 
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            //on verifie si la facture est certifiée
            if (_repo.GetById(invoiceId).IsCertified)
            {
                MessageBox.Show("Cette facture est certifiée et ne peut pas être modifiée.",
                    "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // Récupération des données existantes
                var repo = new InvoiceRepository();
                var invoice = repo.GetById(invoiceId);
                var items = repo.GetItems(invoiceId).ToList();
                var taxes = repo.GetGlobalCustomTaxes(invoiceId);

                // Ouvre le formulaire de modification avec les données existantes
                InvoiceForm form = new InvoiceForm(_state, invoice, items, taxes);

                if (form.ShowDialog() == DialogResult.OK)
                {
                    // La facture a été mise à jour dans la base via le formulaire
                    LoadInvoices(); // Recharge la grille pour afficher les modifications
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la modification : {ex.Message}", 
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void Dgv_Invoices_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvInvoices.CurrentRow != null)
            {
                if (int.TryParse(dgvInvoices.CurrentRow.Cells["Id"].Value?.ToString(), out int id))
                {
                    _selectedInvoiceId = id;
                }
                else
                {
                    _selectedInvoiceId = null;
                }
            }
        }

        private void Btn_PrintInvoice_Click(object sender, EventArgs e)
        {
            if (_selectedInvoiceId == null)
            {
                MessageBox.Show("Veuillez sélectionner une facture à imprimer.", "Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var entreprise = _state.Company;

                // Récupération de la facture
                var invoice = _repo.GetById(_selectedInvoiceId.Value);
                if (invoice == null)
                {
                    MessageBox.Show("Facture introuvable.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Récupération des lignes de facture et des taxes
                var items = _repo.GetItems(_selectedInvoiceId.Value).ToList();
                // Récupération des taxes globales au format sûr
                var globalTaxes = _repo.GetGlobalCustomTaxesSafe(_selectedInvoiceId.Value);


                // Récupération des informations du client lié à la facture
                var client = _clientRepo.GetByCompteClient(invoice.CompteClient);
                if (client == null)
                {
                    MessageBox.Show("Client introuvable pour cette facture.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Génération du nom et du chemin du fichier PDF
                string fileName = $"Facture_{invoice.NumeroFacture}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                string folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string filePath = Path.Combine(folder, fileName);

                // Utilisation du générateur de PDF
                var generator = new PdfInvoiceGenerator();
                generator.GenerateInvoicePdf(entreprise, invoice, client, items,new List<CustomTax>(),globalTaxes, filePath);

                // Ouverture automatique du PDF
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = filePath,
                    UseShellExecute = true
                });

                MessageBox.Show($"Facture exportée avec succès dans :\n{filePath}",
                    "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la génération de la facture PDF : " + ex.Message,
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ValidatePhoneFormat(string template, string phone)
        {
            string value = phone?.Trim() ?? string.Empty;
            string normalizedTemplate = template?.Trim().ToUpperInvariant() ?? string.Empty;

            bool isValid;

            switch (normalizedTemplate)
            {
                case "B2B":
                case "B2C":
                case "B2G":
                    // 10 chiffres, sans espace ni préfixe international
                    isValid = Regex.IsMatch(
                        value,
                        @"\A(?:01|05|07|21|27)\d{8}\z",
                        RegexOptions.CultureInvariant);

                    if (!isValid)
                    {
                        MessageBox.Show(
                            "Pour les factures B2B, B2C et B2G, le téléphone  doit contenir exactement 10 chiffres, " +
                            "sans espace ni préfixe +225/00225, et commencer par 01, 05, 07, 21 ou 27.",
                            "Téléphone du client invalide",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }

                    return isValid;

                case "B2F":
                    // Préfixe international obligatoire ; tous les caractères après le préfixe doivent être des chiffres.
                    isValid = Regex.IsMatch(
                        value,
                        @"\A(?:\+\d+|00\d+)\z",
                        RegexOptions.CultureInvariant);

                    if (!isValid)
                    {
                        MessageBox.Show(
                            "Pour une facture B2F, le téléphone doit commencer par + ou 00, " +
                            "puis contenir uniquement des chiffres, sans espace.",
                            "Téléphone invalide",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }

                    return isValid;

                default:
                    return true;
            }
        }
    }
}
