using FactureFne.Models;
using FactureFne.Services;
using System;
using System.Drawing;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FactureFne.Controls
{
    public partial class ApiSettingsControl : UserControl
    {
        private readonly AppState _state;
        private string code;
        private string codevrai="Travail";

        public ApiSettingsControl(AppState state)
        {
            InitializeComponent();
            _state = state ?? new AppState();
            LoadData();
        }

        private void LoadData()
        {
            txtApiUrl.Text = _state.Api?.BaseUrl ?? "http://54.247.95.108/ws";
            txtToken.Text = _state.Api?.Token ?? "";
            numTimeout.Value = _state.Api != null ? _state.Api.TimeoutSeconds : 30;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtApiUrl.Text))
            {
                MessageBox.Show("Veuillez renseigner l'URL de l'API.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ShowStatus(" URL API vide, enregistrement annulé.", Color.Red);
                return;
            }

            _state.Api.BaseUrl = txtApiUrl.Text.Trim();
            _state.Api.Token = txtToken.Text.Trim();
            _state.Api.TimeoutSeconds = (int)numTimeout.Value;

            StateStore.Save(_state);

            ShowStatus($" Paramètres API enregistrés à {DateTime.Now:T}", Color.Red);
            MessageBox.Show("Paramètres API enregistrés ", "API", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }


        //private async void btnTestConnexion_Click(object sender, EventArgs e)
        //{
        //    string url = txtApiUrl.Text.Trim();
        //    ShowStatus($" Test de connexion en cours vers {url}...", Color.Orange);

        //    try
        //    {
        //        using (HttpClient client = new HttpClient())
        //        {
        //            client.Timeout = TimeSpan.FromSeconds((int)numTimeout.Value);

        //            string token = txtToken.Text.Trim();
        //            if (!string.IsNullOrEmpty(token))
        //            {
        //                client.DefaultRequestHeaders.Authorization =
        //                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        //            }

        //            HttpResponseMessage response = await client.GetAsync(url);

        //            ShowStatus($"Réponse API : {(int)response.StatusCode} {response.ReasonPhrase}", Color.Green);

        //            if (response.IsSuccessStatusCode)
        //                MessageBox.Show("Connexion API réussie ", "Test API", MessageBoxButtons.OK, MessageBoxIcon.Information);
        //            else
        //                MessageBox.Show($"Réponse reçue : {(int)response.StatusCode} {response.ReasonPhrase}",
        //                    "Test API", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //        }
        //    }
        //    catch (TaskCanceledException)
        //    {
        //        ShowStatus(" Timeout atteint. Le serveur n'a pas répondu.", Color.Red);
        //        MessageBox.Show("La requête a expiré (timeout).", "Timeout", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //    }
        //    catch (Exception ex)
        //    {

        //        ShowStatus($" Erreur : {ex.Message}", Color.Red);
        //        MessageBox.Show("Erreur de connexion : " + ex.Message, "Test API", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }
        //}


        private void ApiSettingsControl_Load(object sender, EventArgs e)
        {

        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            // Valeurs par défaut
            txtApiUrl.Text = "http://54.247.95.108/ws";
            txtToken.Text = "";
            numTimeout.Value = 30;

            ShowStatus("Champs réinitialisés avec succès !", Color.Green) ;
        }

        private void ShowStatus(string message, Color color)
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = color;
            lblStatus.Visible = true;

            // Redémarre le timer
            statusTimer.Stop();
            statusTimer.Start();
        }

        private void statusTimer_Tick(object sender, EventArgs e)
        {
            lblStatus.Visible = false;
            statusTimer.Stop(); // on arrête le timer après masquage
        }

        private void btnCheck_Click(object sender, EventArgs e)
        {
            code = textCode.Text;
           bool etat = txtToken.Visible;

          if (String.IsNullOrWhiteSpace(code) && (etat==true))
            {
                txtToken.Visible = false;
                return;
            }
            else if (String.IsNullOrWhiteSpace(code))
            {
                MessageBox.Show("Champ Vide ! \n Veuillez renseigner le code d'activation ");
                textCode.Focus();
                return;
            }
            else if(code==codevrai)
            {
                txtToken.Visible = true;
                txtToken.UseSystemPasswordChar = false;
                return;
            }

            MessageBox.Show($"le code d'activation est Incorrect", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            ((Form1)this.ParentForm).ShowPage(new InvoiceControl(_state));

        }
    }
}
