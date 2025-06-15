using System;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace Cleaner
{
    partial class Login_Form
    {
        /// <summary>
        /// Variável de designer necessária.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpar os recursos que estão sendo usados.
        /// </summary>
        /// <param name="disposing">true se for necessário descartar os recursos gerenciados; caso contrário, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código gerado pelo Windows Form Designer

        /// <summary>
        /// Método necessário para suporte ao Designer - não modifique 
        /// o conteúdo deste método com o editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Login_Form));
            this.Login_Bordless = new Guna.UI2.WinForms.Guna2BorderlessForm(this.components);
            this.UncLogo_Login = new Guna.UI2.WinForms.Guna2PictureBox();
            this.UserLogin_Text = new Guna.UI2.WinForms.Guna2TextBox();
            this.Login_Buton = new Guna.UI2.WinForms.Guna2Button();
            this.Error01_Label = new Guna.UI2.WinForms.Guna2HtmlLabel();
            ((System.ComponentModel.ISupportInitialize)(this.UncLogo_Login)).BeginInit();
            this.SuspendLayout();
            // 
            // Login_Bordless
            // 
            this.Login_Bordless.ContainerControl = this;
            this.Login_Bordless.DockIndicatorTransparencyValue = 0.6D;
            this.Login_Bordless.ResizeForm = false;
            this.Login_Bordless.TransparentWhileDrag = true;
            // 
            // UncLogo_Login
            // 
            this.UncLogo_Login.Image = ((System.Drawing.Image)(resources.GetObject("UncLogo_Login.Image")));
            this.UncLogo_Login.ImageRotate = 0F;
            this.UncLogo_Login.Location = new System.Drawing.Point(149, 40);
            this.UncLogo_Login.Name = "UncLogo_Login";
            this.UncLogo_Login.Size = new System.Drawing.Size(147, 108);
            this.UncLogo_Login.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.UncLogo_Login.TabIndex = 60;
            this.UncLogo_Login.TabStop = false;
            // 
            // UserLogin_Text
            // 
            this.UserLogin_Text.Animated = true;
            this.UserLogin_Text.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.UserLogin_Text.BorderRadius = 5;
            this.UserLogin_Text.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.UserLogin_Text.DefaultText = "";
            this.UserLogin_Text.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.UserLogin_Text.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.UserLogin_Text.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.UserLogin_Text.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.UserLogin_Text.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.UserLogin_Text.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.UserLogin_Text.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.UserLogin_Text.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(130)))), ((int)(((byte)(130)))), ((int)(((byte)(130)))));
            this.UserLogin_Text.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.UserLogin_Text.Location = new System.Drawing.Point(76, 180);
            this.UserLogin_Text.Name = "UserLogin_Text";
            this.UserLogin_Text.PlaceholderText = "USER";
            this.UserLogin_Text.SelectedText = "";
            this.UserLogin_Text.Size = new System.Drawing.Size(290, 38);
            this.UserLogin_Text.TabIndex = 61;
            // 
            // Login_Buton
            // 
            this.Login_Buton.Animated = true;
            this.Login_Buton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(16)))), ((int)(((byte)(16)))), ((int)(((byte)(17)))));
            this.Login_Buton.BorderColor = System.Drawing.Color.Transparent;
            this.Login_Buton.BorderRadius = 5;
            this.Login_Buton.BorderThickness = 1;
            this.Login_Buton.Cursor = System.Windows.Forms.Cursors.Default;
            this.Login_Buton.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.Login_Buton.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.Login_Buton.DisabledState.FillColor = System.Drawing.Color.DarkGray;
            this.Login_Buton.FillColor = System.Drawing.Color.White;
            this.Login_Buton.Font = new System.Drawing.Font("Verdana", 9F);
            this.Login_Buton.ForeColor = System.Drawing.Color.Black;
            this.Login_Buton.Location = new System.Drawing.Point(76, 224);
            this.Login_Buton.Name = "Login_Buton";
            this.Login_Buton.Size = new System.Drawing.Size(288, 38);
            this.Login_Buton.TabIndex = 62;
            this.Login_Buton.Text = "Continue";
            this.Login_Buton.Click += new System.EventHandler(this.Guna2Button1_Click);
            // 
            // Error01_Label
            // 
            this.Error01_Label.BackColor = System.Drawing.Color.Transparent;
            this.Error01_Label.ForeColor = System.Drawing.Color.White;
            this.Error01_Label.Location = new System.Drawing.Point(12, 331);
            this.Error01_Label.Name = "Error01_Label";
            this.Error01_Label.Size = new System.Drawing.Size(12, 15);
            this.Error01_Label.TabIndex = 63;
            this.Error01_Label.Text = "...";
            // 
            // Login_Form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(15)))), ((int)(((byte)(15)))));
            this.ClientSize = new System.Drawing.Size(436, 358);
            this.Controls.Add(this.Error01_Label);
            this.Controls.Add(this.Login_Buton);
            this.Controls.Add(this.UserLogin_Text);
            this.Controls.Add(this.UncLogo_Login);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Login_Form";
            this.Text = "Login";
            this.Load += new System.EventHandler(this.Login_Load);
            ((System.ComponentModel.ISupportInitialize)(this.UncLogo_Login)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Guna.UI2.WinForms.Guna2BorderlessForm Login_Bordless;
        private Guna.UI2.WinForms.Guna2PictureBox UncLogo_Login;
        private Guna.UI2.WinForms.Guna2Button Login_Buton;
        private Guna.UI2.WinForms.Guna2TextBox UserLogin_Text;
        private Guna.UI2.WinForms.Guna2HtmlLabel Error01_Label;
    }
}