using FactureFne.Models;
using FactureFne.Services;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace FactureFne.Controls
{
    public partial class SecurityForm : Form
    {
        private readonly AppState _state;
        private readonly AuthRepository _authRepo;
        private readonly int _userId;
        private readonly Button _togglePasswordButton;

        public SecurityForm(AppState state)
        {
            InitializeComponent();

            _state = state;
            _authRepo = new AuthRepository(DatabaseHelper.GetDatabasePath("Users.db"));

            txtPassword.UseSystemPasswordChar = true;

            _togglePasswordButton = new Button
            {
                Name = "btnTogglePassword",
                Text = "Afficher",
                Location = new Point(txtPassword.Right + 5, txtPassword.Top),
                Size = new Size(75, txtPassword.Height),
                TabStop = false,
                UseVisualStyleBackColor = true
            };
            _togglePasswordButton.Click += TogglePasswordVisibility_Click;
            Controls.Add(_togglePasswordButton);

            button2.DialogResult = DialogResult.Cancel;
            CancelButton = button2;

            if (_state == null || _state.CurrentUser == null)
            {
                MessageBox.Show(
                    "Aucun utilisateur connecté.",
                    "Sécurité",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            _userId = _state.CurrentUser.Id;
            txtUsername.Text = _state.CurrentUser.Username;
        }

        private void TogglePasswordVisibility_Click(object sender, EventArgs e)
        {
            txtPassword.UseSystemPasswordChar = !txtPassword.UseSystemPasswordChar;
            _togglePasswordButton.Text =
                txtPassword.UseSystemPasswordChar ? "Afficher" : "Masquer";

            txtPassword.Focus();
            txtPassword.SelectionStart = txtPassword.TextLength;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show(
                    "Le nom d'utilisateur est obligatoire.",
                    "Validation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtUsername.Focus();
                return;
            }

            bool changePassword = !string.IsNullOrWhiteSpace(password);
            if (changePassword && password.Length < 6)
            {
                MessageBox.Show(
                    "Le mot de passe doit contenir au moins 6 caractères.",
                    "Validation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtPassword.Focus();
                return;
            }

            if (_authRepo.UsernameExists(username, _userId))
            {
                MessageBox.Show(
                    "Ce nom d'utilisateur est déjà utilisé.",
                    "Validation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtUsername.Focus();
                return;
            }

            try
            {
                _authRepo.UpdateUsername(_userId, username);

                if (changePassword)
                    _authRepo.UpdatePassword(_userId, password);

                _state.CurrentUser.Username = username;

                MessageBox.Show(
                    "Les informations de sécurité ont été enregistrées avec succès.",
                    "Enregistrement terminé",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Impossible d'enregistrer les informations de sécurité : " + ex.Message,
                    "Erreur",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
