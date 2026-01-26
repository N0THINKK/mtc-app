namespace mtc_app.features.authentication.presentation.screens
{
    partial class LoginForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblTitle = new mtc_app.shared.presentation.components.AppLabel();
            this.txtUsername = new mtc_app.shared.presentation.components.AppInput();
            this.txtPassword = new mtc_app.shared.presentation.components.AppInput();
            this.btnLogin = new mtc_app.shared.presentation.components.AppButton();
            this.btnExit = new mtc_app.shared.presentation.components.AppButton();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = mtc_app.shared.presentation.styles.AppColors.Surface;
            this.panel1.Controls.Add(this.btnExit);
            this.panel1.Controls.Add(this.btnLogin);
            this.panel1.Controls.Add(this.txtPassword);
            this.panel1.Controls.Add(this.txtUsername);
            this.panel1.Controls.Add(this.lblTitle);
            this.panel1.Location = new System.Drawing.Point(0, 0); // Reset to Top-Left
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(440, 360);
            this.panel1.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Location = new System.Drawing.Point(80, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(280, 32);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "DIGITAL ANDON LOGIN";
            this.lblTitle.Type = mtc_app.shared.presentation.components.AppLabel.LabelType.Header1;
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // txtUsername
            // 
            this.txtUsername.LabelText = "Username";
            this.txtUsername.Location = new System.Drawing.Point(40, 70);
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Size = new System.Drawing.Size(360, 85);
            this.txtUsername.TabIndex = 2;
            this.txtUsername.InputType = mtc_app.shared.presentation.components.AppInput.InputTypeEnum.Text;
            // 
            // txtPassword
            // 
            this.txtPassword.LabelText = "Password";
            this.txtPassword.Location = new System.Drawing.Point(40, 150);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(360, 85);
            this.txtPassword.TabIndex = 4;
            this.txtPassword.InputType = mtc_app.shared.presentation.components.AppInput.InputTypeEnum.Password;
            // 
            // btnLogin
            // 
            this.btnLogin.Location = new System.Drawing.Point(40, 240);
            this.btnLogin.Name = "btnLogin";
            this.btnLogin.Size = new System.Drawing.Size(360, 45);
            this.btnLogin.TabIndex = 5;
            this.btnLogin.Text = "LOGIN";
            this.btnLogin.Type = mtc_app.shared.presentation.components.AppButton.ButtonType.Primary;
            this.btnLogin.Click += new System.EventHandler(this.btnLogin_Click);
            // 
            // btnExit
            // 
            this.btnExit.Location = new System.Drawing.Point(170, 300);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(100, 30);
            this.btnExit.TabIndex = 6;
            this.btnExit.Text = "Exit App";
            this.btnExit.Type = mtc_app.shared.presentation.components.AppButton.ButtonType.Secondary;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // LoginForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = mtc_app.shared.presentation.styles.AppColors.Background;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "LoginForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Login";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private mtc_app.shared.presentation.components.AppLabel lblTitle;
        private mtc_app.shared.presentation.components.AppInput txtUsername;
        private mtc_app.shared.presentation.components.AppInput txtPassword;
        private mtc_app.shared.presentation.components.AppButton btnLogin;
        private mtc_app.shared.presentation.components.AppButton btnExit;
    }
}