namespace FactureFne
{
    partial class Form1
    {
        /// <summary>
        /// Variable nécessaire au concepteur.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur Windows Form

        /// <summary>
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.mainMenu = new System.Windows.Forms.MenuStrip();
            this.contentHost = new System.Windows.Forms.Panel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.menuToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.entrepriseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clientToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aPIToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.factureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.articlesMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.SecuriteMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.menuHistorique = new System.Windows.Forms.ToolStripMenuItem();
            this.menuNewFacture = new System.Windows.Forms.ToolStripMenuItem();
            this.mainMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainMenu
            // 
            this.mainMenu.BackColor = System.Drawing.Color.Transparent;
            this.mainMenu.Dock = System.Windows.Forms.DockStyle.None;
            this.mainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuToolStripMenuItem,
            this.menuHistorique,
            this.menuNewFacture});
            this.mainMenu.Location = new System.Drawing.Point(2, 2);
            this.mainMenu.Name = "mainMenu";
            this.mainMenu.Size = new System.Drawing.Size(351, 35);
            this.mainMenu.TabIndex = 0;
            this.mainMenu.Text = "menuStrip1";
            // 
            // contentHost
            // 
            this.contentHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentHost.Location = new System.Drawing.Point(0, 39);
            this.contentHost.Name = "contentHost";
            this.contentHost.Size = new System.Drawing.Size(1290, 697);
            this.contentHost.TabIndex = 1;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.Green;
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1290, 39);
            this.panel1.TabIndex = 2;
            // 
            // menuToolStripMenuItem
            // 
            this.menuToolStripMenuItem.BackColor = System.Drawing.Color.Transparent;
            this.menuToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.entrepriseToolStripMenuItem,
            this.clientToolStripMenuItem,
            this.aPIToolStripMenuItem,
            this.factureToolStripMenuItem,
            this.articlesMenu,
            this.SecuriteMenu});
            this.menuToolStripMenuItem.Font = new System.Drawing.Font("Arial Narrow", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.menuToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("menuToolStripMenuItem.Image")));
            this.menuToolStripMenuItem.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.menuToolStripMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.menuToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Transparent;
            this.menuToolStripMenuItem.Name = "menuToolStripMenuItem";
            this.menuToolStripMenuItem.Size = new System.Drawing.Size(91, 31);
            this.menuToolStripMenuItem.Text = "Menu";
            // 
            // entrepriseToolStripMenuItem
            // 
            this.entrepriseToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("entrepriseToolStripMenuItem.Image")));
            this.entrepriseToolStripMenuItem.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.entrepriseToolStripMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.entrepriseToolStripMenuItem.Name = "entrepriseToolStripMenuItem";
            this.entrepriseToolStripMenuItem.Size = new System.Drawing.Size(215, 46);
            this.entrepriseToolStripMenuItem.Text = "Entreprise";
            this.entrepriseToolStripMenuItem.Click += new System.EventHandler(this.menuEntreprise_Click);
            // 
            // clientToolStripMenuItem
            // 
            this.clientToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("clientToolStripMenuItem.Image")));
            this.clientToolStripMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.clientToolStripMenuItem.Name = "clientToolStripMenuItem";
            this.clientToolStripMenuItem.Size = new System.Drawing.Size(215, 46);
            this.clientToolStripMenuItem.Text = "Clients";
            this.clientToolStripMenuItem.Click += new System.EventHandler(this.menuClients_Click);
            // 
            // aPIToolStripMenuItem
            // 
            this.aPIToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("aPIToolStripMenuItem.Image")));
            this.aPIToolStripMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.aPIToolStripMenuItem.Name = "aPIToolStripMenuItem";
            this.aPIToolStripMenuItem.Size = new System.Drawing.Size(215, 46);
            this.aPIToolStripMenuItem.Text = "API";
            this.aPIToolStripMenuItem.Click += new System.EventHandler(this.menuApi_Click);
            // 
            // factureToolStripMenuItem
            // 
            this.factureToolStripMenuItem.BackColor = System.Drawing.Color.Transparent;
            this.factureToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("factureToolStripMenuItem.Image")));
            this.factureToolStripMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.factureToolStripMenuItem.Name = "factureToolStripMenuItem";
            this.factureToolStripMenuItem.Size = new System.Drawing.Size(215, 46);
            this.factureToolStripMenuItem.Text = "Import / Export";
            this.factureToolStripMenuItem.Click += new System.EventHandler(this.menuFacture_Click);
            // 
            // articlesMenu
            // 
            this.articlesMenu.Image = ((System.Drawing.Image)(resources.GetObject("articlesMenu.Image")));
            this.articlesMenu.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.articlesMenu.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.articlesMenu.Name = "articlesMenu";
            this.articlesMenu.Size = new System.Drawing.Size(215, 46);
            this.articlesMenu.Text = "Articles";
            this.articlesMenu.Click += new System.EventHandler(this.menuArticles_Click);
            // 
            // SecuriteMenu
            // 
            this.SecuriteMenu.Image = ((System.Drawing.Image)(resources.GetObject("SecuriteMenu.Image")));
            this.SecuriteMenu.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.SecuriteMenu.Name = "SecuriteMenu";
            this.SecuriteMenu.Size = new System.Drawing.Size(215, 46);
            this.SecuriteMenu.Text = "Sécurité";
            this.SecuriteMenu.Visible = false;
            this.SecuriteMenu.Click += new System.EventHandler(this.SecuriteMenu_Click);
            // 
            // menuHistorique
            // 
            this.menuHistorique.BackColor = System.Drawing.Color.Transparent;
            this.menuHistorique.Font = new System.Drawing.Font("Arial Narrow", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.menuHistorique.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.menuHistorique.Image = ((System.Drawing.Image)(resources.GetObject("menuHistorique.Image")));
            this.menuHistorique.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.menuHistorique.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.menuHistorique.Name = "menuHistorique";
            this.menuHistorique.Size = new System.Drawing.Size(127, 31);
            this.menuHistorique.Text = "Historique";
            this.menuHistorique.Click += new System.EventHandler(this.menuHistorique_Click);
            // 
            // menuNewFacture
            // 
            this.menuNewFacture.BackColor = System.Drawing.Color.MediumBlue;
            this.menuNewFacture.Font = new System.Drawing.Font("Arial Narrow", 14.25F, System.Drawing.FontStyle.Bold);
            this.menuNewFacture.ForeColor = System.Drawing.Color.White;
            this.menuNewFacture.Image = ((System.Drawing.Image)(resources.GetObject("menuNewFacture.Image")));
            this.menuNewFacture.Name = "menuNewFacture";
            this.menuNewFacture.Size = new System.Drawing.Size(125, 31);
            this.menuNewFacture.Text = "Facturation";
            this.menuNewFacture.Click += new System.EventHandler(this.menuNewFacture_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1290, 736);
            this.Controls.Add(this.contentHost);
            this.Controls.Add(this.mainMenu);
            this.Controls.Add(this.panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Fact_Fne";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.mainMenu.ResumeLayout(false);
            this.mainMenu.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip mainMenu;
        private System.Windows.Forms.ToolStripMenuItem menuToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem entrepriseToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem clientToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aPIToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem factureToolStripMenuItem;
        private System.Windows.Forms.Panel contentHost;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ToolStripMenuItem menuHistorique;
        private System.Windows.Forms.ToolStripMenuItem menuNewFacture;
        private System.Windows.Forms.ToolStripMenuItem articlesMenu;
        private System.Windows.Forms.ToolStripMenuItem SecuriteMenu;
    }
}

