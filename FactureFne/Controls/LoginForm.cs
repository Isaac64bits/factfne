using FactureFne.Models;
using FactureFne.Services; // là où est ton AuthRepository
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FactureFne
{
    public partial class LoginForm : Form
    {
        private readonly AuthRepository _authRepo;
        private readonly AppState _state;

        public LoginForm(AppState state)
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.FixedSingle; // taille fixe
            this.MaximizeBox = false; // désactive le bouton agrandir
                                      //  chemin vers ta base SQLite
            _state = state;                  // <-- stocke l'état global
            _authRepo = new AuthRepository("Users.db");

            txtPassword.UseSystemPasswordChar = true;
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Veuillez remplir tous les champs.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            //  Vérification via AuthRepository
            bool isValid = _authRepo.ValidateUser(username, password);

            if (isValid)
            {
                var user = _authRepo.GetUserByUsername(username);

                if (user == null)
                {
                    MessageBox.Show(
                        "Erreur interne : utilisateur introuvable après authentification.",
                        "Erreur",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return;
                }

                _state.SetCurrentUser(user); // ✔ méthode prévue dans AppState

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Nom d’utilisateur ou mot de passe incorrect.",
                    "Erreur de connexion", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            //if (isValid)
            //{
            //    // Récupère les infos complètes de l'utilisateur depuis la DB
            //    var user = _authRepo.GetUserByUsername(username);  // méthode à créer si pas déjà

            //    // Met à jour CurrentUser dans AppState
            //    _state.CurrentUser = user;

            //    // Connexion réussie → ouvrir ton Form principal

            //    this.DialogResult = DialogResult.OK; // <-- permet au Program.cs de savoir que c'est validé
            //    this.Close();
            //}

        }

        //Bouton Quitter
        private void button2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            // Par défaut : mot de passe masqué
            txtPassword.UseSystemPasswordChar = true;

            // PictureBox setup (si pas déjà fait dans le designer)
            picTogglePassword.SizeMode = PictureBoxSizeMode.StretchImage;
            picTogglePassword.Cursor = Cursors.Hand;

            // Charger l'icône (ressources ou fallback fichier)
            picTogglePassword.Image = LoadResourceImage("eye_closed") ?? LoadImageFromFileFallback("eye_closed.png");
        }

        private void picTogglePassword_Click(object sender, EventArgs e)
        {
            if (txtPassword.UseSystemPasswordChar)
            {
                txtPassword.UseSystemPasswordChar = false;
                picTogglePassword.Image = LoadResourceImage("eye_open") ?? LoadImageFromFileFallback("eye_closed.png");
            }
            else
            {
                txtPassword.UseSystemPasswordChar = true;
                picTogglePassword.Image = LoadResourceImage("eye_closed") ?? LoadImageFromFileFallback("eye_open.png");
            }
        }

        private Image LoadResourceImage(string resourceName)
        {
            try
            {
                // Essaie via ResourceManager (retourne null si pas trouvé)
                var obj = Properties.Resources.ResourceManager.GetObject(resourceName);
                return obj as Image;
            }
            catch
            {
                return null;
            }
        }

        private Image LoadImageFromFileFallback(string fileName)
        {
            try
            {
                string p = System.IO.Path.Combine(Application.StartupPath, fileName);
                if (System.IO.File.Exists(p))
                    return Image.FromFile(p);
            }
            catch { /* ignore */ }
            return null;
        }

    }
}
