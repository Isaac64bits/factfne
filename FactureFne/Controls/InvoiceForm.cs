using Dapper;
using FactureFne.Models;
using FactureFne.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace FactureFne.Controls
{
    public partial class InvoiceForm : Form
    {
        private readonly ClientRepository _clientRepo;
        private readonly ArticlesRepository _articlesRepo;
        private readonly InvoiceRepository _invoiceRepo;
        private readonly Timer _articleSearchTimer;
        private readonly Timer _searchTimer;
        private readonly AppState _state;
        private bool _isEditingFromGrid = false;
        public event EventHandler InvoiceUpdated;
        private int indexArticleEnEdition = -1;


        private decimal _totalHtLignes = 0m;
        private bool _isUpdatingRemise = false;
        private bool _isSelectingArticle = false;
        private bool _isSelectingClient = false;
        public int? _editingInvoiceId;
        private bool _isEditMode = false;
        private int _invoiceId = 0;

        public InvoiceForm(AppState state, int? invoiceId = null)
        {
            InitializeComponent();
            _state = state;

            // <-- initialiser les repositories d'abord
            _invoiceRepo = new InvoiceRepository();
            _clientRepo = new ClientRepository();
            _articlesRepo = new ArticlesRepository();

            // stocke l'id si on est en édition
            if (invoiceId.HasValue)
            {
                _isEditMode = true;
                _invoiceId = invoiceId.Value;
                LoadInvoiceForEdit(_invoiceId); // méthode à créer
            }// Charger les données de la facture
            
            cbx_TypeFact.SelectedIndexChanged += Cbx_TypeFact_SelectedIndexChanged;


            // Timers de recherche
            _articleSearchTimer = new Timer { Interval = 300 };
            _articleSearchTimer.Tick += ArticleSearchTimer_Tick;

            _searchTimer = new Timer { Interval = 300 };
            _searchTimer.Tick += SearchTimer_Tick;

            // Événements
            txtRefArticle.TextChanged += TxtRefArticle_TextChanged;
            txtRefArticle.Leave += TxtRefArticle_Leave;
            comboClients.TextChanged += ComboClients_TextChanged;
            comboClients.SelectedIndexChanged += ComboClients_SelectedIndexChanged;

            // Événement pour modifier un article dans le DataGridView
            //dgv_Article.CellDoubleClick += dgv_Article_CellDoubleClick;

            txt_SommeTauxRemise.TextChanged += Txt_SommeTauxRemise_TextChanged;
            txt_MontantRemiseTHT.TextChanged += Txt_MontantRemiseTHT_TextChanged;

            //Evenement lors de la saisie de la taxe sur TTC
            TauxTaxe_TTC.TextChanged += (s, e) => UpdateTotals();

            //  Ici on génère le numéro de facture automatique
            txt_NumFact.Text = GetNextInvoiceNumber();

        }

        //private void cbx_TypeFact_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    if (cbx_TypeFact.Text == "AVOIR" || cbx_TypeFact.SelectedItem.ToString() == "AVOIR")
        //    {
        //        labRefFne.Visible = true;
        //        txtRefFne.Visible = true;
        //    }
        //    else
        //    {
        //        labRefFne.Visible = false;
        //        txtRefFne.Visible = false;
        //    }
        //}


        //private string GetNextInvoiceNumber()
        //{
        //    string nextNumber = "FAC0000001"; // Numéro par défaut pour la première facture
        //    string connectionString = DatabaseHelper.GetConnectionString("invoices.db");

        //    using (var connection = new SQLiteConnection(connectionString))
        //    {
        //        connection.Open();

        //        // On récupère le dernier Id (le plus grand)
        //        int lastId = connection.ExecuteScalar<int?>("SELECT MAX(Id) FROM Invoices;") ?? 0;

        //        // Le prochain Id
        //        int nextId = lastId + 1;

        //        // On forme le numéro de facture : FAC + 7 chiffres
        //        nextNumber = $"FAC{nextId:D7}";
        //    }

        //    return nextNumber;
        //}

        private void Cbx_TypeFact_SelectedIndexChanged(object sender, EventArgs e)
        {
            string typeFact = cbx_TypeFact.Text?.Trim().ToUpper() ?? "";

            // --- Gestion de la visibilité du champ Référence FNE ---
            bool isAvoir = (typeFact == "AVOIR");
            labRefFne.Visible = isAvoir;
            txtRefFne.Visible = isAvoir;
           
            if(_isEditMode)
             {
                return; // Ne pas mettre à jour le numéro si on est en mode édition
            }
            // --- Mise à jour automatique du numéro de facture ---
            txt_NumFact.Text = GetNextInvoiceNumber();
        }

        private string GetNextInvoiceNumber()
        {
            // --- Déterminer le préfixe selon le type de facture ---
            string typeFact = cbx_TypeFact.Text?.Trim().ToUpper() ?? "VENTE";
            string prefix;

            if (typeFact == "AVOIR")
                prefix = "AV";
            else if (typeFact == "DEV" || typeFact == "DEVIS")
                prefix = "DEV";
            else
                prefix = "FAC"; // par défaut pour les factures normales

            string nextNumber = $"{prefix}0000001"; // numéro par défaut si table vide

            string connectionString = DatabaseHelper.GetConnectionString("invoices.db");

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // On récupère le dernier numéro avec ce préfixe
                string query = "SELECT NumeroFacture FROM Invoices WHERE NumeroFacture LIKE @prefix ORDER BY Id DESC LIMIT 1;";
                string lastNumber = connection.ExecuteScalar<string>(query, new { prefix = prefix + "%" });

                if (!string.IsNullOrEmpty(lastNumber))
                {
                    // On extrait la partie numérique du numéro (après le préfixe)
                    string numericPart = lastNumber.Substring(prefix.Length);
                    if (int.TryParse(numericPart, out int lastId))
                    {
                        int nextId = lastId + 1;
                        nextNumber = $"{prefix}{nextId:D7}";
                    }
                }
            }

            return nextNumber;
        }


        private void LoadInvoiceForEdit(int invoiceId)
        {
            // Récupère la facture
            var invoice = _invoiceRepo.GetById(invoiceId);
            if (invoice == null)
            {
                MessageBox.Show("Facture introuvable.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Pré-remplir champs d'en-tête
            txt_NumFact.Text = invoice.NumeroFacture;
            txt_RefFact.Text = invoice.ReferenceFacture ?? "";
            txtRefFne.Text = invoice.ParentReference ?? "";
            txt_SommeTauxRemise.Text = invoice.Remisetht.ToString("N2"); // pourcentage stocké
            txt_MontantRemiseTHT.Text = (invoice.MontantHtTotal * (invoice.Remisetht / 100m)).ToString("N2"); // approximatif

            txtCompteCli.Text = invoice.CompteClient ?? "";

            // Si tu veux sélectionner le client dans comboClients basé sur Compte,
            // tu peux chercher le client
            var client = _clientRepo.GetByCode(invoice.CompteClient);
            if (client != null)
            {
                // Si comboClients est lié par DataSource, il faudra rechercher l'index
                // Ici on essaye une sélection simple
                comboClients.Text = client.Libelle;
                txtCompteCli.Text = client.Compte;
            }

            // Charger les lignes
            dgv_Article.Rows.Clear();
            var items = _invoiceRepo.GetItems(invoiceId).OrderByDescending(i => i.Id).ToList();
            foreach (var it in items)
            {
                // Ajoute la ligne avec les colonnes attendues (adapte l'ordre au dgv)
                int rowIndex = dgv_Article.Rows.Add(
                    it.CodeArticle ?? "",
                    it.Libelle ?? "",
                    it.Quantite,
                    it.Unite ?? "",
                    it.PrixUnitaire,
                    it.Remise,
                    (it.Quantite * it.PrixUnitaire) - (it.Quantite * it.PrixUnitaire * (it.Remise / 100m)) // totalHT ligne brut - remise article
                );

                // Remplir le Tag (ArticleTaxInfo) pour réutiliser tes calculs
                decimal extraTaxPct = 0m;
                string extraTaxName = "";

                // Récupère les taxes personnalisées pour la ligne si présentes
                // InvoiceRepository.GetItems() remplit item.CustomTaxes si possible
                if (it.CustomTaxes != null && it.CustomTaxes.Any())
                {
                    // On prend la première custom tax comme taxe supplémentaire par ligne (si défini ainsi)
                    var tax = it.CustomTaxes.First();
                    extraTaxPct = tax.Amount;
                    extraTaxName = tax.Name;
                }

                dgv_Article.Rows[rowIndex].Tag = new ArticleTaxInfo
                {
                    PrincipalTaxCode = it.TypeTaxe ?? "",
                    ExtraTaxName = extraTaxName,
                    ExtraTaxRate = extraTaxPct
                };
            }

            // Recharge les taxes globales (si tu as GlobalCustomTaxes)
            var globalTaxes = _invoiceRepo.GetGlobalCustomTaxes(invoiceId);
            if (globalTaxes != null && globalTaxes.Any())
            {
                // Si la taxe TTC globale est stockée ici, cherche la taxe qui correspond au NomTaxe_TTC
                var t = globalTaxes.FirstOrDefault();
                if (t != null)
                {
                    NomTaxe_TTC.Text = t.Name;
                    TauxTaxe_TTC.Text = t.Amount.ToString("N2");
                }
            }

            // Après remplissage, mettre à jour les totaux
            UpdateTotals();
        }
        // ---------- Bouton Ajouter Article ----------
        private void btn_AjoutArticle_Click(object sender, EventArgs e)
        {
            string reference = txtRefArticle.Text.Trim();
            string designation = txtDesignationArticle.Text.Trim();
            string unite = txtUniteArticle.Text.Trim();

            // Vérifications de base
            if (string.IsNullOrWhiteSpace(reference) && string.IsNullOrWhiteSpace(designation))
            {
                MessageBox.Show("Veuillez renseigner la référence et la désignation de l'article.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(reference) && !string.IsNullOrWhiteSpace(designation) || !string.IsNullOrWhiteSpace(reference) && string.IsNullOrWhiteSpace(designation))
            {
                if (string.IsNullOrWhiteSpace(unite))
                {
                    MessageBox.Show("Veuillez renseigner l'unité de l'article.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if(string.IsNullOrWhiteSpace(txtRemiseSurArticle.ToString()))
                {
                    MessageBox.Show("Veuillez renseigner la remise de l'article.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
               
            }

            
            // Vérifications numériques
            if (!decimal.TryParse(txtQuantiteArticle.Text, out decimal quantite) || quantite < 0)
            {
                MessageBox.Show("Quantité invalide.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtPrixUnitaireHtArticle.Text, out decimal prixUnitaire) || prixUnitaire < 0)
            {
                MessageBox.Show("Prix unitaire invalide.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal remise = 0;
            decimal.TryParse(txtRemiseSurArticle.Text, out remise);

            // Taxes
            string taxCode = TauxImpositionArticle.SelectedItem?.ToString();
            string nomTaxeSup = NomTaxe_Article.Visible ? NomTaxe_Article.Text.Trim() : "";
            decimal tauxTaxeSup = NomTaxe_Article.Visible && decimal.TryParse(TauxTaxe_Article.Text, out var taux) ? taux : 0m;

            // Calcul du total HT de la ligne
            decimal totalHt = quantite * prixUnitaire * (1 - remise / 100m);

            // === GESTION DU MODE AJOUT OU MODIFICATION ===
            if (indexArticleEnEdition >= 0)
            {

                //  Mise à jour de la ligne existante
                var row = dgv_Article.Rows[indexArticleEnEdition];
                row.Cells["Col_reference"].Value = reference;
                row.Cells["Col_designation"].Value = designation;
                row.Cells["Col_quantite"].Value = quantite;
                row.Cells["Col_unite"].Value = unite;
                row.Cells["Col_prixunitaire"].Value = prixUnitaire;
                row.Cells["Col_remise"].Value = remise;
                row.Cells["Col_total"].Value = totalHt;

                row.Tag = new ArticleTaxInfo
                {
                    PrincipalTaxCode = taxCode ?? "",
                    ExtraTaxName = nomTaxeSup,
                    ExtraTaxRate = tauxTaxeSup
                };

                // Réinitialisation du mode édition
                _isEditingFromGrid = false; //  Active la recherche auto

                indexArticleEnEdition = -1;
                btn_AjoutArticle.Text = "Ajouter";
            }
            else
            {
                //  Ajout d’un nouvel article
                int rowIndex = dgv_Article.Rows.Add(reference, designation, quantite, unite, prixUnitaire, remise, totalHt);
                dgv_Article.Rows[rowIndex].Tag = new ArticleTaxInfo
                {
                    PrincipalTaxCode = taxCode ?? "",
                    ExtraTaxName = nomTaxeSup,
                    ExtraTaxRate = tauxTaxeSup
                };
            }

            // Mise à jour du champ total ligne
            txtTotalhtArticle.Text = totalHt.ToString("N6");

            // Réinitialisation des champs après ajout/modification
            txtRefArticle.Clear();
            txtDesignationArticle.Clear();
            txtQuantiteArticle.Text = "0";
            txtPrixUnitaireHtArticle.Text = "0";
            txtRemiseSurArticle.Clear();
            txtTotalhtArticle.Clear();
            NomTaxe_Article.Clear();
            TauxTaxe_Article.Clear();
            txtUniteArticle.Clear();

            TauxImpositionArticle.SelectedItem = "TVA";

            label_AjtNomArticle.Visible = false;
            NomTaxe_Article.Visible = false;
            label_AjtTauxArticle.Visible = false;
            TauxTaxe_Article.Visible = false;

            // Recalcule le total général
            UpdateTotals();

            // Remet le focus sur le champ référence
            //txtRefArticle.Focus();
        }

         //---------- Double-clic pour modifier une ligne d'article ----------
        private void dgv_Article_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            _isEditingFromGrid = true; //  Désactive la recherche auto

            try
            {
                if (e.RowIndex < 0 || e.RowIndex >= dgv_Article.Rows.Count)
                    return;


                var row = dgv_Article.Rows[e.RowIndex];

                // Récupère les valeurs
                txtRefArticle.Text = row.Cells["Col_reference"].Value?.ToString();
                txtDesignationArticle.Text = row.Cells["Col_designation"].Value?.ToString();
                txtQuantiteArticle.Text = row.Cells["Col_quantite"].Value?.ToString();
                txtUniteArticle.Text = row.Cells["Col_unite"].Value?.ToString();
                txtPrixUnitaireHtArticle.Text = row.Cells["Col_prixunitaire"].Value?.ToString();
                txtRemiseSurArticle.Text = row.Cells["Col_remise"].Value?.ToString();
                txtTotalhtArticle.Text = row.Cells["Col_total"].Value?.ToString();

                //taxes principal
                var taxCode = (row.Tag as ArticleTaxInfo)?.PrincipalTaxCode;
                if (!string.IsNullOrWhiteSpace(taxCode))
                {
                    TauxImpositionArticle.SelectedItem = taxCode;
                }
                else
                {
                    TauxImpositionArticle.SelectedIndex = -1;
                }

                // Taxes
                var taxInfo = row.Tag as ArticleTaxInfo;
                if (taxInfo != null)
                {
                    //TauxImpositionArticle.Text = taxInfo.PrincipalTaxCode;
                    NomTaxe_Article.Text = taxInfo.ExtraTaxName;
                    TauxTaxe_Article.Text = taxInfo.ExtraTaxRate.ToString();
                }

                // Mémorise l’index de la ligne à modifier
                indexArticleEnEdition = e.RowIndex;

                // Change le texte du bouton pour signaler qu'on est en mode modification
                btn_AjoutArticle.Text = "Modifier";

                //txtRefArticle.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement de l’article à modifier : " + ex.Message,
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void TxtRefArticle_TextChanged(object sender, EventArgs e)
        {
            if (_isSelectingArticle || _isEditingFromGrid) return; //  Empêche la recherche pendant l’édition
            _articleSearchTimer.Stop();
            _articleSearchTimer.Start();
        }

        private void TxtRefArticle_Leave(object sender, EventArgs e)
        {
            if (_isEditingFromGrid) return;

            string reference = txtRefArticle.Text.Trim();
            if (string.IsNullOrEmpty(reference)) return;

            var article = _articlesRepo.GetByReference(reference);

            if (article != null)
            {
                txtDesignationArticle.Text = article.Designation;
                txtQuantiteArticle.Text = article.Quantite.ToString();
                txtUniteArticle.Text = article.Unite;
                txtPrixUnitaireHtArticle.Text = article.Prix.ToString("0.00");

            }
            else
            {
                txtDesignationArticle.Clear();
                txtUniteArticle.Clear();
                txtPrixUnitaireHtArticle.Text = 0m.ToString();
                txtRemiseSurArticle.Clear();
                txtQuantiteArticle.Text = 0m.ToString();
            }
        }

        private async void ArticleSearchTimer_Tick(object sender, EventArgs e)
        {
            if (_isEditingFromGrid) return; // Ne pas lancer la recherche pendant un double-clic

            _articleSearchTimer.Stop();
            string refSearch = txtRefArticle.Text.Trim();
            if (string.IsNullOrWhiteSpace(refSearch)) return;

            try
            {
                var article = await Task.Run(() => _articlesRepo.GetByReference(refSearch));
                if (article != null)
                {
                    _isSelectingArticle = true;
                    txtDesignationArticle.Text = article.Designation;
                    txtQuantiteArticle.Text = article.Quantite.ToString();
                    txtUniteArticle.Text = article.Unite;
                    txtPrixUnitaireHtArticle.Text = article.Prix.ToString("N6");
                }
                else
                {
                    txtDesignationArticle.Clear();
                    txtQuantiteArticle.Text = 0m.ToString();
                    txtUniteArticle.Clear();
                    txtPrixUnitaireHtArticle.Text = 0m.ToString("N6");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la recherche d'article : " + ex.Message);
            }
            finally
            {
                _isSelectingArticle = false;
            }
        }

        private void ComboClients_TextChanged(object sender, EventArgs e)
        {
            if (_isSelectingClient) return;
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private async void SearchTimer_Tick(object sender, EventArgs e)
        {
            _searchTimer.Stop();
            string search = comboClients.Text.Trim();

            if (search.Length < 2) return;

            try
            {
                comboClients.SelectedIndexChanged -= ComboClients_SelectedIndexChanged;
                _isSelectingClient = true;

                var results = await Task.Run(() => _clientRepo.SearchByName(search));
                comboClients.DataSource = results.ToList();
                comboClients.DisplayMember = "Libelle";
                comboClients.ValueMember = "Id";
                comboClients.DroppedDown = true;
                comboClients.SelectedIndex = -1;
                comboClients.Text = search;
                comboClients.SelectionStart = comboClients.Text.Length;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur : " + ex.Message);
            }
            finally
            {
                _isSelectingClient = false;
                comboClients.SelectedIndexChanged += ComboClients_SelectedIndexChanged;
            }
        }

        private void ComboClients_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboClients.SelectedItem is ClientInfo client)
            {
                _isSelectingClient = true;
                txtEmail.Text = client.Email ?? "";
                txtCompteCli.Text = client.Compte ?? "";
                _isSelectingClient = false;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _searchTimer?.Stop();
            _articleSearchTimer?.Stop();
            base.OnFormClosing(e);
        }

        private void AjtTaxeArticle_Click(object sender, EventArgs e)
        {
            bool visible = NomTaxe_Article.Visible;
            label_AjtNomArticle.Visible = !visible;
            NomTaxe_Article.Visible = !visible;
            label_AjtTauxArticle.Visible = !visible;
            TauxTaxe_Article.Visible = !visible;
        }

        private void AjtTaxeTTC_Click(object sender, EventArgs e)
        {
            bool visible = NomTaxe_TTC.Visible;
            label_AjtNomTaxeTTC.Visible = !visible;
            NomTaxe_TTC.Visible = !visible;
            label_AjtTauxTTC.Visible = !visible;
            TauxTaxe_TTC.Visible = !visible;
        }

        private decimal GetTaxRateFromCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return 0m;

            switch (code.Trim().ToUpperInvariant())
            {
                case "TVA":   // TVA normal
                    return 18m;
                case "TVAB":  // TVA réduit
                    return 09m;
                case "TVAC":  // TVA exec conv
                case "TVAD":  // TVA exec leg
                    return 00m;
                default:
                    return 0m;
            }
        }


        private decimal _currentTotalHT = 0m;
        private decimal _currentTotalHTapresRemiseGlobale = 0m;
        private decimal _currentTotalTVA = 0m;
        private decimal _currentTotalRemise = 0m;
        private decimal _currentTotalAutresTaxes = 0m;
        private decimal _currentTotalTTC = 0m;
        private decimal _currentNetAPayer = 0m;

        private void UpdateTotals()
        {
            var lines = new List<(decimal lineHt, decimal tauxPrincipalPct, decimal extraTaxPct)>();
            decimal totalHtLines = 0m;

            // --- Parcours des lignes du DataGridView ---
            foreach (DataGridViewRow row in dgv_Article.Rows)
            {
                if (row.IsNewRow) continue;

                decimal.TryParse(row.Cells["Col_quantite"]?.Value?.ToString(), out decimal quantite);
                decimal.TryParse(row.Cells["Col_prixUnitaire"]?.Value?.ToString(), out decimal prixUnitaire);
                decimal.TryParse(row.Cells["Col_remise"]?.Value?.ToString(), out decimal remisePct);

                decimal lineBrut = quantite * prixUnitaire;
                decimal lineRemiseAmt = lineBrut * (remisePct / 100m);
                decimal lineNet = lineBrut - lineRemiseAmt;

                var taxInfo = row.Tag as ArticleTaxInfo ?? new ArticleTaxInfo();
                decimal tauxPrincipalPct = 0m;
                if (!string.IsNullOrWhiteSpace(taxInfo.PrincipalTaxCode))
                    tauxPrincipalPct = GetTaxRateFromCode(taxInfo.PrincipalTaxCode);

                decimal extraTaxPct = taxInfo.ExtraTaxRate;

                lines.Add((lineNet, tauxPrincipalPct, extraTaxPct));
                totalHtLines += lineNet;

                txtTotalhtArticle.Text = totalHtLines.ToString("N2");
            }

            _totalHtLignes = totalHtLines;

            // --- Application de la remise globale ---
            decimal remiseGlobaleMontant = 0m;
            if (decimal.TryParse(txt_SommeTauxRemise.Text, out decimal remiseGlobalePct) && remiseGlobalePct > 0)
                remiseGlobaleMontant = _totalHtLignes * (remiseGlobalePct / 100m);
            else if (decimal.TryParse(txt_MontantRemiseTHT.Text, out decimal remiseMontant) && remiseMontant > 0)
                remiseGlobaleMontant = remiseMontant;

            if (remiseGlobaleMontant > _totalHtLignes)
                remiseGlobaleMontant = _totalHtLignes;

            decimal totalHtApresRemiseGlobale = _totalHtLignes - remiseGlobaleMontant;
            decimal factor = (_totalHtLignes > 0) ? (totalHtApresRemiseGlobale / _totalHtLignes) : 0m;

            // --- Calcul des taxes ---
            decimal totalTva = 0m;
            decimal totalAutresTaxes = 0m;
            decimal sommeTauxSupp = 0m;

            foreach (var (lineHt, tauxPrincipalPct, extraTaxPct) in lines)
            {
                decimal lineAfterGlobal = lineHt * factor;

                // Taxe principale (TVA)
                decimal taxePrincipale = lineAfterGlobal * (tauxPrincipalPct / 100m);
                totalTva += taxePrincipale;

                // Taxe supplémentaire
                if (extraTaxPct > 0)
                {
                    decimal taxeSupp = (lineAfterGlobal + taxePrincipale) * (extraTaxPct / 100m);
                    totalAutresTaxes += taxeSupp;
                    sommeTauxSupp += extraTaxPct;
                }
            }

            // --- Taxe sur le TTC (optionnelle) ---
            if (!string.IsNullOrWhiteSpace(NomTaxe_TTC.Text)
                && decimal.TryParse(TauxTaxe_TTC.Text, out decimal tauxTtcSup)
                && tauxTtcSup > 0)
            {
                decimal totalTTCIntermediaire = totalHtApresRemiseGlobale + totalTva;
                decimal taxeSupTTC = totalTTCIntermediaire * (tauxTtcSup / 100m);
                totalAutresTaxes += taxeSupTTC;
                sommeTauxSupp += tauxTtcSup;
            }

            // --- Total TTC et Net à payer ---
            decimal totalTTC = totalHtApresRemiseGlobale + totalTva;
            decimal totalNetAPayer = totalTTC + totalAutresTaxes;

            // --- Mise à jour des variables internes (utiles pour GetInvoiceInfo) ---
            _currentTotalHT = _totalHtLignes;
            _currentTotalHTapresRemiseGlobale = totalHtApresRemiseGlobale;
            _currentTotalTVA = totalTva;
            _currentTotalRemise = remiseGlobaleMontant;
            _currentTotalAutresTaxes = totalAutresTaxes;
            _currentTotalTTC = totalTTC;
            _currentNetAPayer = totalNetAPayer;

            // --- Arrondi des montants ---
            decimal totalHtArrondi = Math.Round(_totalHtLignes, 2);
            decimal remiseGlobaleArrondi = Math.Round(remiseGlobaleMontant, 2);
            decimal totalTvaArrondi = Math.Round(totalTva, 2);
            decimal totalAutresTaxesArrondi = Math.Round(totalAutresTaxes, 2);
            decimal totalTtcArrondi = Math.Round(totalTTC, 2);
            decimal netAPayerArrondi = Math.Round(totalNetAPayer, 2);

            // --- Mise à jour des montants à l’écran ---
            TOTO_TotalHT.Text = totalHtArrondi.ToString("N2");
            TOTO_Remise.Text = remiseGlobaleArrondi.ToString("N2");
            TOTO_Taxe.Text = totalTvaArrondi.ToString("N2");
            TOTO_AutresTaxes.Text = totalAutresTaxesArrondi.ToString("N2");
            TOTO_TotalTTC.Text = totalTtcArrondi.ToString("N2");
            TOTO_TotalaPayer.Text = netAPayerArrondi.ToString("N2");

            // --- Mise à jour des taux ---
            Somme_TauxTotalHt.Text = "-";

            //  Calcul du taux de remise global (en % du total HT)
            Somme_TauxRemises.Text = _totalHtLignes > 0
                ? ((remiseGlobaleMontant / _totalHtLignes) * 100m).ToString("N2") + " %"
                : "0 %";

            //  Calcul du taux de TVA global (en % du total HT)
            Somme_TauxTotalTaxes.Text = _totalHtLignes > 0
                ? ((totalTva / _totalHtLignes) * 100m).ToString("N2") + " %"
                : "0 %";

            //  Taux des autres taxes
            Somme_TauxAutresTaxes.Text = sommeTauxSupp.ToString("N2") + " %";

            //  Pas de pourcentage pour le TTC et le Net à Payer → juste un tiret
            Somme_TauxTotalTTC.Text = "-";
            Somme_TauxTotalaPayer.Text = "-";
        }

        public InvoiceInfo GetInvoiceInfo()
        {
            return new InvoiceInfo
            {
                Id = _editingInvoiceId ?? 0,
                NumeroFacture = txt_NumFact.Text.Trim(),
                TypeFacture = cbx_TypeFact.SelectedItem?.ToString() ?? "VENTE",
                CompteClient = (comboClients.SelectedItem as ClientInfo)?.Compte ?? "",
                ReferenceFacture = txt_RefFact.Text.Trim(),
                ParentReference = txtRefFne.Text.Trim(),
                DateFacture = DateTime.Now.ToString("dd-MM-yyyy"),
                MontantHtTotal = decimal.TryParse(TOTO_TotalHT.Text, out var ht) ? ht : 0,
                MontantTvaTotal = decimal.TryParse(TOTO_Taxe.Text, out var tva) ? tva : 0,
                Remisetht = decimal.TryParse(txt_SommeTauxRemise.Text, out var rem) ? rem : 0,
                IsCertified = false
            };
        }


        // ---------- Synchronisation % <-> montant ----------
        private void Txt_SommeTauxRemise_TextChanged(object sender, EventArgs e)
        {
            if (_isUpdatingRemise) return;
            _isUpdatingRemise = true;

            if (decimal.TryParse(txt_SommeTauxRemise.Text, out decimal pct) && _totalHtLignes > 0)
            {
                decimal montant = _totalHtLignes * (pct / 100m);
                txt_MontantRemiseTHT.Text = montant.ToString("N2");
            }
            else
            {
                txt_MontantRemiseTHT.Text = "0.00";
            }

            _isUpdatingRemise = false;
            UpdateTotals();
        }

        private void Txt_MontantRemiseTHT_TextChanged(object sender, EventArgs e)
        {
            if (_isUpdatingRemise) return;
            _isUpdatingRemise = true;

            if (decimal.TryParse(txt_MontantRemiseTHT.Text, out decimal montant) && _totalHtLignes > 0)
            {
                decimal pct = (_totalHtLignes > 0) ? (montant / _totalHtLignes * 100m) : 0m;
                txt_SommeTauxRemise.Text = pct.ToString("N2");
            }
            else
            {
                txt_SommeTauxRemise.Text = "0.00";
            }

            _isUpdatingRemise = false;
            UpdateTotals();
        }
        //------------------------- Bouton Enregistrer ---------------------
        private bool securite()
        {
            // Vérifie le type de facture
            if (cbx_TypeFact.SelectedItem != null && cbx_TypeFact.SelectedItem.ToString() == "AVOIR")
            {
                if (string.IsNullOrWhiteSpace(txtRefFne.Text))
                {
                    MessageBox.Show(
                        "Veuillez renseigner la référence de la facture d'origine pour un avoir.",
                        "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtRefFne.Focus();
                    return false;
                }
            }

            // Vérifie la sélection du client
            if (comboClients.SelectedItem == null || string.IsNullOrWhiteSpace(txtCompteCli.Text))
            {
                MessageBox.Show(
                    "Veuillez sélectionner un client.",
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                comboClients.Focus();
                return false;
            }

            // Vérifie qu'il y a au moins un article
            if (dgv_Article.Rows.Count == 0 || _totalHtLignes <= 0)
            {
                MessageBox.Show(
                    "Veuillez ajouter au moins un article à la facture.",
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRefArticle.Focus();
                return false;
            }

            return true; // tout est bon
        }


        private void btn_EnregistrerFacture_Click(object sender, EventArgs e)
        {
            if (!securite()) return;

            try
            {
                // Récupère toutes les données de l'UI
                var invoice = GetInvoiceInfo();
                var items = GetInvoiceItems();
                var globalTaxes = GetGlobalTaxes();

                var repo = new InvoiceRepository();

                if (_editingInvoiceId > 0)
                {
                    // Mode modification
                    invoice.Id = _editingInvoiceId.Value;   // S'assure que l'ID est bien renseigné
                    repo.UpdateInvoice(invoice, items, globalTaxes);
                    MessageBox.Show("Facture mise à jour avec succès !", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    // Mode création
                    int newId = repo.InsertInvoice(invoice, items, globalTaxes);
                    invoice.Id = newId;
                    MessageBox.Show("Facture enregistrée avec succès !", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                // Tout OK : ferme la fenêtre et signale au parent
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                // Affiche l'erreur et ne ferme pas
                MessageBox.Show("Erreur lors de l'enregistrement : " + ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        //private void SauvegarderFacture(int? idFactureParam = null)
        //{
        //    string connectionString = DatabaseHelper.GetConnectionString("invoices.db");
        //    using (var connection = new SQLiteConnection(connectionString))
        //    {
        //        connection.Open();
        //        using (var transaction = connection.BeginTransaction())
        //        {
        //            try
        //            {
        //                // 1️⃣ Récupération des lignes du DataGridView
        //                var itemList = new List<dynamic>();
        //                decimal totalHtLines = 0m;

        //                foreach (DataGridViewRow row in dgv_Article.Rows)
        //                {
        //                    if (row.IsNewRow) continue;

        //                    decimal.TryParse(row.Cells["Col_quantite"]?.Value?.ToString(), out decimal quantite);
        //                    decimal.TryParse(row.Cells["Col_prixUnitaire"]?.Value?.ToString(), out decimal prixUnitaire);
        //                    decimal.TryParse(row.Cells["Col_remise"]?.Value?.ToString(), out decimal remisePct);

        //                    decimal lineBrut = quantite * prixUnitaire;
        //                    decimal lineRemiseAmt = lineBrut * (remisePct / 100m);
        //                    decimal lineNet = lineBrut - lineRemiseAmt;

        //                    var taxInfo = row.Tag as ArticleTaxInfo ?? new ArticleTaxInfo();
        //                    decimal tauxPrincipalPct = !string.IsNullOrWhiteSpace(taxInfo.PrincipalTaxCode)
        //                        ? GetTaxRateFromCode(taxInfo.PrincipalTaxCode)
        //                        : 0m;

        //                    itemList.Add(new
        //                    {
        //                        CodeArticle = row.Cells["Col_reference"]?.Value?.ToString() ?? "",
        //                        Libelle = row.Cells["Col_designation"]?.Value?.ToString() ?? "",
        //                        Quantite = quantite,
        //                        Unite = row.Cells["Col_unite"]?.Value?.ToString() ?? "",
        //                        PrixUnitaire = prixUnitaire,
        //                        RemisePct = remisePct,
        //                        LineNet = lineNet,
        //                        TaxCode = taxInfo.PrincipalTaxCode ?? "",
        //                        TauxPrincipalPct = tauxPrincipalPct,
        //                        ExtraTaxName = taxInfo.ExtraTaxName ?? "",
        //                        ExtraTaxPct = taxInfo.ExtraTaxRate
        //                    });

        //                    totalHtLines += lineNet;
        //                }

        //                // 2️⃣ Calcul des remises et taxes globales
        //                decimal remiseGlobalePct = 0m;
        //                if (decimal.TryParse(txt_SommeTauxRemise.Text, out decimal pct) && pct > 0)
        //                    remiseGlobalePct = pct;

        //                decimal remiseGlobaleMontant = totalHtLines * (remiseGlobalePct / 100m);
        //                if (remiseGlobaleMontant > totalHtLines)
        //                    remiseGlobaleMontant = totalHtLines;

        //                decimal totalHtAfterGlobal = totalHtLines - remiseGlobaleMontant;
        //                decimal factor = (totalHtLines > 0) ? (totalHtAfterGlobal / totalHtLines) : 0m;

        //                decimal totalTva = 0m;
        //                decimal totalAutresTaxes = 0m;

        //                foreach (var it in itemList)
        //                {
        //                    decimal lineAfterGlobal = it.LineNet * factor;
        //                    totalTva += lineAfterGlobal * (it.TauxPrincipalPct / 100m);
        //                    if (it.ExtraTaxPct > 0)
        //                        totalAutresTaxes += lineAfterGlobal * (it.ExtraTaxPct / 100m);
        //                }

        //                if (!string.IsNullOrWhiteSpace(NomTaxe_TTC.Text) &&
        //                    decimal.TryParse(TauxTaxe_TTC.Text, out decimal tauxTtcSup) &&
        //                    tauxTtcSup > 0)
        //                {
        //                    decimal totalTTCIntermediaire = totalHtAfterGlobal + totalTva + totalAutresTaxes;
        //                    decimal taxeSup = totalTTCIntermediaire * (tauxTtcSup / 100m);
        //                    totalAutresTaxes += taxeSup;
        //                }

        //                decimal totalTTCFinal = totalHtAfterGlobal + totalTva + totalAutresTaxes;

        //                // 3️⃣ Données facture
        //                var invoiceData = new
        //                {
        //                    TypeFacture = cbx_TypeFact?.SelectedItem?.ToString() ?? "VENTE",
        //                    NumeroFacture = string.IsNullOrWhiteSpace(txt_NumFact.Text)
        //                        ? "NF-" + DateTime.Now.ToString("yyyyMMddHHmmss")
        //                        : txt_NumFact.Text.Trim(),
        //                    DateFacture = DateTime.Now.ToString("yyyy-MM-dd"),
        //                    CompteClient = txtCompteCli.Text.Trim(),
        //                    ReferenceFacture = txt_RefFact.Text.Trim(),
        //                    ParentReference = string.IsNullOrWhiteSpace(txtRefFne.Text) ? null : txtRefFne.Text.Trim(),
        //                    MontantHtTotal = totalHtLines,
        //                    MontantTvaTotal = totalTva,
        //                    Remisetht = remiseGlobalePct,
        //                    IsCertified = 0,
        //                    ImportDate = DateTime.Now.ToString("o")
        //                };

        //                int idFacture;

        //                // 4️⃣ INSERT ou UPDATE
        //                if (_isEditMode && idFactureParam.HasValue)
        //                {
        //                    // Vérifie si la facture existe
        //                    var existing = connection.QueryFirstOrDefault<int?>(
        //                        "SELECT Id FROM Invoices WHERE Id = @Id",
        //                        new { Id = idFactureParam.Value }, transaction);

        //                    if (existing == null)
        //                        throw new Exception("Impossible de modifier : la facture n'existe plus.");

        //                    // --- UPDATE ---
        //                    string updateQuery = @"
        //                UPDATE Invoices SET
        //                    TypeFacture = @TypeFacture,
        //                    NumeroFacture = @NumeroFacture,
        //                    DateFacture = @DateFacture,
        //                    CompteClient = @CompteClient,
        //                    ReferenceFacture = @ReferenceFacture,
        //                    ParentReference = @ParentReference,
        //                    MontantHtTotal = @MontantHtTotal,
        //                    MontantTvaTotal = @MontantTvaTotal,
        //                    Remisetht = @Remisetht,
        //                    IsCertified = @IsCertified,
        //                    ImportDate = @ImportDate
        //                WHERE Id = @Id;";

        //                    connection.Execute(updateQuery, new
        //                    {
        //                        Id = idFactureParam.Value,
        //                        invoiceData.TypeFacture,
        //                        invoiceData.NumeroFacture,
        //                        invoiceData.DateFacture,
        //                        invoiceData.CompteClient,
        //                        invoiceData.ReferenceFacture,
        //                        invoiceData.ParentReference,
        //                        invoiceData.MontantHtTotal,
        //                        invoiceData.MontantTvaTotal,
        //                        invoiceData.Remisetht,
        //                        invoiceData.IsCertified,
        //                        invoiceData.ImportDate
        //                    }, transaction);

        //                    idFacture = idFactureParam.Value;

        //                    // Nettoyage des anciennes lignes
        //                    connection.Execute("DELETE FROM CustomTaxes WHERE InvoiceItemId IN (SELECT Id FROM InvoiceItems WHERE InvoiceId = @InvoiceId);",
        //                        new { InvoiceId = idFacture }, transaction);
        //                    connection.Execute("DELETE FROM InvoiceItems WHERE InvoiceId = @InvoiceId;",
        //                        new { InvoiceId = idFacture }, transaction);
        //                    connection.Execute("DELETE FROM GlobalCustomTaxes WHERE InvoiceId = @InvoiceId;",
        //                        new { InvoiceId = idFacture }, transaction);
        //                }
        //                else
        //                {
        //                    // --- INSERT ---
        //                    string insertInvoiceQuery = @"
        //                INSERT INTO Invoices
        //                (TypeFacture, NumeroFacture, DateFacture, CompteClient, ReferenceFacture,
        //                 ParentReference, MontantHtTotal, MontantTvaTotal, Remisetht, IsCertified, ImportDate)
        //                VALUES
        //                (@TypeFacture, @NumeroFacture, @DateFacture, @CompteClient, @ReferenceFacture,
        //                 @ParentReference, @MontantHtTotal, @MontantTvaTotal, @Remisetht, @IsCertified, @ImportDate);
        //                SELECT last_insert_rowid();";

        //                    idFacture = connection.ExecuteScalar<int>(insertInvoiceQuery, invoiceData, transaction);
        //                }

        //                // 5️⃣ Insertion des lignes
        //                foreach (var it in itemList)
        //                {
        //                    int itemId = connection.ExecuteScalar<int>(@"
        //                INSERT INTO InvoiceItems
        //                (InvoiceId, CodeArticle, Libelle, Quantite, Unite, PrixUnitaire, Remise, TypeTaxe, TauxTva)
        //                VALUES
        //                (@InvoiceId, @CodeArticle, @Libelle, @Quantite, @Unite, @PrixUnitaire, @Remise, @TypeTaxe, @TauxTva);
        //                SELECT last_insert_rowid();",
        //                        new
        //                        {
        //                            InvoiceId = idFacture,
        //                            CodeArticle = it.CodeArticle,
        //                            Libelle = it.Libelle,
        //                            Quantite = it.Quantite,
        //                            Unite = it.Unite,
        //                            PrixUnitaire = it.PrixUnitaire,
        //                            Remise = it.RemisePct,
        //                            TypeTaxe = it.TaxCode,
        //                            TauxTva = it.TauxPrincipalPct
        //                        }, transaction);

        //                    if (it.ExtraTaxPct > 0 && !string.IsNullOrWhiteSpace(it.ExtraTaxName))
        //                    {
        //                        connection.Execute(@"
        //                    INSERT INTO CustomTaxes (InvoiceItemId, Name, Amount)
        //                    VALUES (@InvoiceItemId, @Name, @Amount);",
        //                            new { InvoiceItemId = itemId, Name = it.ExtraTaxName, Amount = it.ExtraTaxPct }, transaction);
        //                    }
        //                }

        //                // 6️⃣ Taxe globale
        //                if (!string.IsNullOrWhiteSpace(NomTaxe_TTC.Text) &&
        //                    decimal.TryParse(TauxTaxe_TTC.Text, out decimal tauxSup) &&
        //                    tauxSup > 0)
        //                {
        //                    connection.Execute(@"
        //                INSERT INTO GlobalCustomTaxes (InvoiceId, Name, Amount)
        //                VALUES (@InvoiceId, @Name, @Amount);",
        //                        new { InvoiceId = idFacture, Name = NomTaxe_TTC.Text, Amount = tauxSup }, transaction);
        //                }

        //                // 7️⃣ Commit final
        //                transaction.Commit();

        //                MessageBox.Show("Facture enregistrée avec succès !", "Succès",
        //                    MessageBoxButtons.OK, MessageBoxIcon.Information);
        //            }
        //            catch (Exception ex)
        //            {
        //                transaction.Rollback();
        //                MessageBox.Show("Erreur : " + ex.Message, "Échec",
        //                    MessageBoxButtons.OK, MessageBoxIcon.Error);
        //            }
        //        }
        //    }
        //}


        private void btn_Effacer_Click(object sender, EventArgs e)
        {
            if (dgv_Article.Rows.Count == 0 &&
                string.IsNullOrWhiteSpace(txtRefArticle.Text) &&
                string.IsNullOrWhiteSpace(txtDesignationArticle.Text))
            {
                MessageBox.Show("Aucune donnée à effacer.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult result = MessageBox.Show(
                "Voulez-vous vraiment effacer tous les articles et les champs ?",
                "Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.No)
                return;

            // Efface toutes les lignes du DataGridView
            dgv_Article.Rows.Clear();

            // Vide les champs article
            txtRefArticle.Clear();
            txtDesignationArticle.Clear();
            txtQuantiteArticle.Clear();
            txtPrixUnitaireHtArticle.Clear();
            txtUniteArticle.Clear();
            txtTotalhtArticle.Clear();
            txtRemiseSurArticle.Clear();

            // Remise globale
            txt_SommeTauxRemise.Clear();
            txt_MontantRemiseTHT.Clear();

            // Taxe article
            NomTaxe_Article.Clear();
            TauxTaxe_Article.Clear();
            label_AjtNomArticle.Visible = false;
            NomTaxe_Article.Visible = false;
            label_AjtTauxArticle.Visible = false;
            TauxTaxe_Article.Visible = false;

            // Taxe TTC
            NomTaxe_TTC.Clear();
            TauxTaxe_TTC.Clear();
            label_AjtNomTaxeTTC.Visible = false;
            NomTaxe_TTC.Visible = false;
            label_AjtTauxTTC.Visible = false;
            TauxTaxe_TTC.Visible = false;

            // Réinitialise les totaux
            UpdateTotals();

            // Repositionne le curseur pour la prochaine saisie
            txtRefArticle.Focus();
        }

        private void Btn_DeleteArt_Click(object sender, EventArgs e)
        {
            // Vérifie qu’il y a au moins une ligne sélectionnée
            if (dgv_Article.CurrentRow == null || dgv_Article.CurrentRow.IsNewRow)
            {
                MessageBox.Show("Veuillez sélectionner une ligne à supprimer.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Confirmation avant suppression
            DialogResult result = MessageBox.Show(
                "Voulez-vous vraiment supprimer cette ligne ?",
                "Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                try
                {
                    // Supprime la ligne sélectionnée
                    dgv_Article.Rows.Remove(dgv_Article.CurrentRow);

                    // Optionnel : recalculer les totaux après suppression
                    UpdateTotals();

                    MessageBox.Show("Ligne supprimée avec succès.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la suppression : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public class GlobalDiscount
        {
            public decimal Percentage { get; set; }  // Si l’utilisateur entre un pourcentage
            public decimal Amount { get; set; }     // Si l’utilisateur entre un montant fixe
        }


        private class ArticleTaxInfo
        {
            public string PrincipalTaxCode { get; set; } = "";
            public string ExtraTaxName { get; set; } = "";
            public decimal ExtraTaxRate { get; set; } = 0m;
        }

        private class CustomTaxGlobal
        {
            public string Name { get; set; } = "";   // Nom de la taxe globale (ex: "TVA", "AIRSI")
            public decimal Amount { get; set; }      // Taux ou montant de la taxe (ex: 5 ou 0.05)
        }

        private void InvoiceForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.DialogResult != DialogResult.OK)
            {
                var result = MessageBox.Show(
                    "Voulez-vous vraiment fermer sans enregistrer ?",
                    "Confirmation",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.No)
                {
                    e.Cancel = true; // Empêche la fermeture
                }
            }
        }

        public InvoiceForm(AppState state, InvoiceInfo invoice, List<InvoiceItem> items, List<CustomTaxDraft> globalTaxes)
      : this(state)
        {
            _editingInvoiceId = invoice.Id;
            _isEditMode = true;

            if (_isEditMode) { cbx_TypeFact.Enabled = false; }


            // En-tête
            txt_NumFact.Text = invoice.NumeroFacture;
            txt_RefFact.Text = invoice.ReferenceFacture ?? "";
            cbx_TypeFact.SelectedItem = invoice.TypeFacture ?? "VENTE";
            txtRefFne.Text = invoice.ParentReference ?? "";

            //  Remise globale
            txt_SommeTauxRemise.Text = invoice.Remisetht.ToString("N2") ?? "";

            // Client
            var client = _clientRepo.GetByCode(invoice.CompteClient);
            if (client != null)
            {   
                comboClients.Text = client.Libelle;
                txtCompteCli.Text = client.Compte ?? "";
            }   

            // Articles
            dgv_Article.Rows.Clear();
            foreach (var it in items)
            {
                int rowIndex = dgv_Article.Rows.Add(
                    it.CodeArticle ?? "",
                    it.Libelle ?? "",
                    it.Quantite,
                    it.Unite ?? "",
                    it.PrixUnitaire,
                    it.Remise,
                    (it.Quantite * it.PrixUnitaire) - (it.Quantite * it.PrixUnitaire * (it.Remise / 100m))
                );

                decimal extraTaxPct = 0m;
                string extraTaxName = "";
                if (it.CustomTaxes != null && it.CustomTaxes.Any())
                {
                    var tax = it.CustomTaxes.First();
                    extraTaxPct = tax.Amount;
                    extraTaxName = tax.Name;
                }

                dgv_Article.Rows[rowIndex].Tag = new ArticleTaxInfo
                {
                    PrincipalTaxCode = it.TypeTaxe ?? "",
                    ExtraTaxName = extraTaxName,
                    ExtraTaxRate = extraTaxPct
                };
            }

            // Taxes globales
            if (globalTaxes != null && globalTaxes.Any())
            {
                var t = globalTaxes.FirstOrDefault();
                if (t != null)
                {
                    NomTaxe_TTC.Text = t.Name;
                    TauxTaxe_TTC.Text = t.Amount.ToString("N2");
                }
            }

            // Recalcul des totaux
            UpdateTotals();
        }

        public List<InvoiceItem> GetInvoiceItems()
        {
            var items = new List<InvoiceItem>();
            foreach (DataGridViewRow row in dgv_Article.Rows)
            {
                if (row.IsNewRow) continue;
                var taxInfo = row.Tag as ArticleTaxInfo;

                decimal quantite = 0, prix = 0, remise = 0;
                decimal.TryParse(row.Cells["Col_quantite"].Value?.ToString(), out quantite);
                decimal.TryParse(row.Cells["Col_prixunitaire"].Value?.ToString(), out prix);
                decimal.TryParse(row.Cells["Col_remise"].Value?.ToString(), out remise);

                var item = new InvoiceItem
                {
                    CodeArticle = row.Cells["Col_reference"].Value?.ToString() ?? "",
                    Libelle = row.Cells["Col_designation"].Value?.ToString() ?? "",
                    Quantite = quantite,
                    Unite = row.Cells["Col_unite"].Value?.ToString() ?? "",
                    PrixUnitaire = prix,
                    Remise = remise,
                    TypeTaxe = taxInfo?.PrincipalTaxCode ?? "",
                    //TauxTva = taxInfo?.PrincipalTaxRate ?? 0,
                    CustomTaxes = (taxInfo != null && !string.IsNullOrWhiteSpace(taxInfo.ExtraTaxName))
                        ? new List<CustomTaxDraft> { new CustomTaxDraft { Name = taxInfo.ExtraTaxName, Amount = taxInfo.ExtraTaxRate } }
                        : new List<CustomTaxDraft>()
                };
                items.Add(item);
            }
            return items;
        }

        public List<CustomTaxDraft> GetGlobalTaxes()
        {
            var taxes = new List<CustomTaxDraft>();
            if (!string.IsNullOrWhiteSpace(NomTaxe_TTC.Text) && decimal.TryParse(TauxTaxe_TTC.Text, out var taux))
            {
                taxes.Add(new CustomTaxDraft { Name = NomTaxe_TTC.Text.Trim(), Amount = taux });
            }
            return taxes;
        }

    }
}
