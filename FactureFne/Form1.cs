using System;
using System.Net.Http;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FactureFne.Models;
using FactureFne.Services;
using FactureFne.Controls;

namespace FactureFne
{
    public partial class Form1 : Form
    {
        private AppState _state;
        private InvoiceControl _invoiceControl;


        //public Form1()
        //{
        //    InitializeComponent();
        //    Text = "Fact_Fne";
        //    this.StartPosition = FormStartPosition.CenterScreen;
        //    this.FormBorderStyle = FormBorderStyle.FixedSingle; // taille fixe
        //    this.MaximizeBox = false; // désactive le bouton agrandir
        //    Form1_Load(this, EventArgs.Empty);

        //}

        public Form1(AppState state)
        {
            InitializeComponent();

            Text = "Fact_Fne";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            _state = state; // ON GARDE LE STATE DU LOGIN

            _invoiceControl = new InvoiceControl(_state);
            ShowPage(_invoiceControl);
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            //_state = StateStore.Load();
            // Ouvre la page Entreprise par défaut
            //ShowPage(new CompanyInfoControl(_state));
            _invoiceControl = new InvoiceControl(_state);
            ShowPage(_invoiceControl);
        }

        public void ShowPage(UserControl page)
        {
            page.Dock = DockStyle.Fill;
            contentHost.Controls.Clear();
            contentHost.Controls.Add(page);
        }

        // Événements menu (à relier depuis le Designer)
        private void menuEntreprise_Click(object sender, EventArgs e)
            => ShowPage(new CompanyInfoControl(_state));

        private void menuClients_Click(object sender, EventArgs e)
            => ShowPage(new ClientsControl(_state));        // (on fera à l’étape 2)

        private void menuApi_Click(object sender, EventArgs e)
            => ShowPage(new ApiSettingsControl(_state));     // (étape 3)

        private void menuFacture_Click(object sender, EventArgs e)
            => ShowPage(new InvoiceControl(_state));         // (étape 4)

        private void menuHistorique_Click(object sender, EventArgs e)
         => ShowPage(new CertifiedInvoicesControl(_state));

        private void menuArticles_Click(object sender, EventArgs e)
         => ShowPage(new ArticlesControl(_state));

        private void menuNewFacture_Click(object sender, EventArgs e)
        {
            InvoiceForm form = new InvoiceForm(_state);
            var result = form.ShowDialog();

            if (result == DialogResult.OK)
            {
                // Ici, on rafraîchit le DataGridView après fermeture du form
                if (contentHost.Controls[0] is InvoiceControl invoiceControl)
                {
                    invoiceControl.LoadInvoices(); // recharge les factures dans le DGV
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            var result = MessageBox.Show("êtes vous sûr de vouloir quitter l'application ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.No)
            {
                e.Cancel = true; // Annule la fermeture du formulaire
            }
        }

        private void SecuriteMenu_Click(object sender, EventArgs e)
        {
            using (var form = new SecurityForm(_state))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    MessageBox.Show(
                        "Veuillez redémarrer l'application pour appliquer les changements.",
                        "Information",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
            }
        }

    }
}


