using FactureFne.Models;
using FactureFne.Services;
using System;
using System.Windows.Forms;

namespace FactureFne.Controls
{
    public partial class SecurityForm : Form
    {
        private readonly AppState _state;
        private readonly AuthRepository _authRepo;
        private readonly int _userId;

        public SecurityForm(AppState state)
        {
            InitializeComponent();

            _state = state; // ✅ OBLIGATOIRE
            _authRepo = new AuthRepository();

            if (_state.CurrentUser == null)
            {
                MessageBox.Show("Utilisateur non connecté.");
                Close();
                return;
            }

            _userId = _state.CurrentUser.Id;
            txtUsername.Text = _state.CurrentUser.Username;
        }



        private void btnSave_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Le nom d'utilisateur est obligatoire.");
                return;
            }

            if (_authRepo.UsernameExists(username, _userId))
            {
                MessageBox.Show("Ce nom d'utilisateur est déjà utilisé.");
                return;
            }

            // DB
            _authRepo.UpdateUsername(_userId, username);

            // MÉMOIRE
            _state.CurrentUser.Username = username;

            if (!string.IsNullOrWhiteSpace(password))
            {
                if (password.Length < 6)
                {
                    MessageBox.Show("Le mot de passe doit contenir au moins 6 caractères.");
                    return;
                }

                _authRepo.UpdatePassword(_userId, password);
            }

            MessageBox.Show(
                "Informations de sécurité mises à jour avec succès.",
                "Succès",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            DialogResult = DialogResult.OK;
            Close();
        }

    }
}
