using FactureFne.Models;
using FactureFne.Services;
using OfficeOpenXml;
using System;
using System.Windows.Forms;
using static System.Windows.Forms.AxHost;

namespace FactureFne
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            var state = StateStore.Load(); // config uniquement;


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //  Initialiser les bases de données AVANT d’ouvrir une fenêtre
            DatabaseHelper.InitializeDatabases();
           
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Vérifier que toutes les DB sont accessibles en écriture
            string[] dbs = { "clients.db", "apiResponses.db", "invoices.db", "Users.db" };
            foreach (var db in dbs)
            {
                if (!DatabaseHelper.IsWritable(db))
                {
                    MessageBox.Show($"Impossible d'écrire dans la base de données '{db}'. Vérifiez les droits dans AppData.",
                                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }


            using (var login = new LoginForm(state))
            {
                // LoginForm doit définir DialogResult = DialogResult.OK en cas de succès
                if (login.ShowDialog() == DialogResult.OK)
                {
                    var mainForm = new Form1(state)
                    {
                        StartPosition = FormStartPosition.CenterScreen
                    };

                    // S'assurer qu'à l'affichage la fenêtre soit normale et bien au premier plan
                    mainForm.Shown += (s, e) =>
                    {
                        try
                        {
                            // remettre état normal si nécessaire
                            if (mainForm.WindowState == FormWindowState.Minimized)
                                mainForm.WindowState = FormWindowState.Normal;

                            // Forcer l'avant-plan : TopMost true temporaire + Activate()
                            mainForm.TopMost = true;
                            mainForm.Activate();
                            // petit délai non bloquant pas nécessaire ici, on remet à false après
                            mainForm.TopMost = false;

                            mainForm.BringToFront();
                        }
                        catch
                        {
                            // ignore pour ne pas casser le démarrage si qch plante
                        }
                    };

                    Application.Run(mainForm);
                }
                else
                {
                    // fermeture propre si login annulé/échoué
                    Application.Exit();
                }
            }
        }
    }
}
