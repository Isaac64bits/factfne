using FactureFne.Models;
using FactureFne.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.AxHost;

namespace FactureFne.Controls
{
    public partial class CompanyInfoControl : UserControl
    {
        private readonly AppState _state;
        private string logoPath;


        public CompanyInfoControl(AppState state)
        {
            InitializeComponent();

            _state = state ?? new AppState(); // sécurité
            LoadData();

            logoPath = Properties.Settings.Default.LogoPath;
            if (File.Exists(logoPath))
            {
                pbxLogo.Image = Image.FromFile(logoPath);
            }
        }



        private void label7_Click(object sender, EventArgs e)
        {

        }

        private decimal ClampToNumericUpDown(decimal value)
        {
            if (value < numCurrencyRate.Minimum) return numCurrencyRate.Minimum;
            if (value > numCurrencyRate.Maximum) return numCurrencyRate.Maximum;
            return value;
        }

        private void LoadData()
        {
            txtPointOfSale.Text = _state.Company.PointOfSale;
            txtEstablishment.Text = _state.Company.Establishment;
            txtCommercialMessage.Text = _state.Company.CommercialMessage;
            txtFooter.Text = _state.Company.Footer;

            // Charger les nouveaux champs
            txtNccEntreprise.Text = _state.Company.NccEntreprise;
            txtclientSellerName.Text = _state.Company.ClientSellerName;
            //txtCodcliAirsi.Text = _state.Company.CodcliAirsi;

            if (cboCurrency.Items.Count == 0)
            {
                cboCurrency.Items.Add("");
                cboCurrency.Items.AddRange(new object[] { "XOF", "USD", "EUR", "JPY", "CAD", "GBP", "AUD", "CNH", "CHF", "HKD", "NZD" });
            }
            cboCurrency.SelectedItem = _state.Company.ForeignCurrency ?? "";
            numCurrencyRate.Value = ClampToNumericUpDown(_state.Company.ForeignCurrencyRate);

        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            _state.Company.PointOfSale = txtPointOfSale.Text.Trim();
            _state.Company.Establishment = txtEstablishment.Text.Trim();
            _state.Company.CommercialMessage = txtCommercialMessage.Text.Trim();
            _state.Company.Footer = txtFooter.Text.Trim();

            _state.Company.NccEntreprise = txtNccEntreprise.Text.Trim();
            _state.Company.ClientSellerName = txtclientSellerName.Text.Trim();
            //_state.Company.CodcliAirsi = txtCodcliAirsi.Text.Trim();

            _state.Company.ForeignCurrency = (cboCurrency.SelectedItem as string) ?? "";
            _state.Company.ForeignCurrencyRate = numCurrencyRate.Value;

            if (string.IsNullOrWhiteSpace(txtNccEntreprise.Text))
            {
                MessageBox.Show("Le NCC Entreprise est obligatoire.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            else
            {
                StateStore.Save(_state);
            MessageBox.Show("Informations de l'entreprise enregistrées avec succès ", "Entreprise", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }

        }

        private void btnEffacer_Click(object sender, EventArgs e)
        {
            if (_state.Company.IsEmpty())
            {
                MessageBox.Show("Tous les champs sont vides ","Infos",MessageBoxButtons.OK,MessageBoxIcon.Exclamation);
            }
            else {

                    var result = MessageBox.Show(
                    "Voulez-vous vraiment effacer toutes les informations de l'entreprise ?",
                    "Confirmation",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                    return; // L'utilisateur a choisi "Non", on ne fait rien

                // Si l'utilisateur a confirmé, on vide les champs
                txtPointOfSale.Clear();
                txtEstablishment.Clear();
                txtCommercialMessage.Clear();
                txtFooter.Clear();
                txtNccEntreprise.Clear();
                txtclientSellerName.Clear();
                //txtCodcliAirsi.Clear();

                // Réinitialiser ComboBox et NumericUpDown
                if (cboCurrency.Items.Count > 0)
                    cboCurrency.SelectedIndex = 0;

                numCurrencyRate.Value = 0;

                // Réinitialiser l'état en mémoire
                _state.Company.PointOfSale = "";
                _state.Company.Establishment = "";
                _state.Company.CommercialMessage = "";
                _state.Company.Footer = "";
                _state.Company.NccEntreprise = "";
                _state.Company.ClientSellerName = "";
                _state.Company.CodcliAirsi = "";
                _state.Company.ForeignCurrency = "";
                _state.Company.ForeignCurrencyRate = 0;

                StateStore.Save(_state);

                MessageBox.Show("Toutes les informations ont été effacées ",
                    "Effacer", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }
        }

        private void txtclientSellerName_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            ((Form1)this.ParentForm).ShowPage(new InvoiceControl(_state));
        }

        private void btnImporterLogo_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Sélectionnez le logo de votre entreprise";
                openFileDialog.Filter = "Fichiers image|*.png;*.jpg;*.jpeg;*.bmp;*.gif";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // Affiche l’image dans le PictureBox
                    pbxLogo.Image = Image.FromFile(openFileDialog.FileName);

                    // Optionnel : stocker le chemin sélectionné pour réutilisation dans la génération du PDF
                    logoPath = openFileDialog.FileName;

                    // --- Sauvegarde pour la prochaine session ---
                    Properties.Settings.Default.LogoPath = logoPath;
                    Properties.Settings.Default.Save();

                    // Message de confirmation
                    MessageBox.Show("Logo enregistré avec succès.", "Confirmation", MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
            }
        }

    }
}
