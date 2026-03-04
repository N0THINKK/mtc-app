namespace mtc_app.features.authentication.presentation.screens
{
    partial class LoginForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.pnlCard = new System.Windows.Forms.Panel();
            this.tblLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblTitle = new mtc_app.shared.presentation.components.AppLabel();
            this.drpRole = new mtc_app.shared.presentation.components.AppInput();
            this.pnlInputSwap = new System.Windows.Forms.Panel();
            this.txtIdentity = new mtc_app.shared.presentation.components.AppInput();
            this.txtPassword = new mtc_app.shared.presentation.components.AppInput();
            this.btnLogin = new mtc_app.shared.presentation.components.AppButton();
            this.btnExit = new mtc_app.shared.presentation.components.AppButton();

            this.pnlCard.SuspendLayout();
            this.tblLayout.SuspendLayout();
            this.pnlInputSwap.SuspendLayout();
            this.SuspendLayout();

            // ---------------------------------------------------------------
            // tblLayout — Single-column vertical stack (the ONLY layout logic)
            // ---------------------------------------------------------------
            this.tblLayout.ColumnCount = 1;
            this.tblLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblLayout.RowCount = 5;
            this.tblLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblLayout.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblLayout.AutoSize = true;
            this.tblLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tblLayout.BackColor = System.Drawing.Color.Transparent;
            this.tblLayout.Name = "tblLayout";
            this.tblLayout.TabIndex = 0;

            this.tblLayout.Controls.Add(this.lblTitle, 0, 0);
            this.tblLayout.Controls.Add(this.drpRole, 0, 1);
            this.tblLayout.Controls.Add(this.pnlInputSwap, 0, 2);
            this.tblLayout.Controls.Add(this.btnLogin, 0, 3);
            this.tblLayout.Controls.Add(this.btnExit, 0, 4);

            // ---------------------------------------------------------------
            // lblTitle — Header, centered in its cell
            // ---------------------------------------------------------------
            this.lblTitle.AutoSize = true;
            this.lblTitle.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lblTitle.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Text = "DIGITAL LOGIN";
            this.lblTitle.Type = mtc_app.shared.presentation.components.AppLabel.LabelType.Header1;
            this.lblTitle.TabIndex = 0;

            // ---------------------------------------------------------------
            // drpRole — Role dropdown (stretches to fill cell width)
            // ---------------------------------------------------------------
            this.drpRole.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.drpRole.Margin = new System.Windows.Forms.Padding(0);
            this.drpRole.LabelText = "Login Sebagai";
            this.drpRole.Name = "drpRole";
            this.drpRole.TabIndex = 1;
            this.drpRole.InputType = mtc_app.shared.presentation.components.AppInput.InputTypeEnum.Dropdown;
            this.drpRole.AllowCustomText = true;

            // ---------------------------------------------------------------
            // pnlInputSwap — Wraps identity + password (one visible at a time)
            // ---------------------------------------------------------------
            this.pnlInputSwap.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.pnlInputSwap.Margin = new System.Windows.Forms.Padding(0);
            this.pnlInputSwap.BackColor = System.Drawing.Color.Transparent;
            this.pnlInputSwap.Name = "pnlInputSwap";
            this.pnlInputSwap.TabIndex = 2;
            this.pnlInputSwap.Controls.Add(this.txtPassword);
            this.pnlInputSwap.Controls.Add(this.txtIdentity);

            // ---------------------------------------------------------------
            // txtIdentity — Text input (Dock.Fill inside wrapper)
            // ---------------------------------------------------------------
            this.txtIdentity.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtIdentity.LabelText = "NIK Operator";
            this.txtIdentity.Name = "txtIdentity";
            this.txtIdentity.TabIndex = 0;
            this.txtIdentity.InputType = mtc_app.shared.presentation.components.AppInput.InputTypeEnum.Text;

            // ---------------------------------------------------------------
            // txtPassword — Password input (Dock.Fill, hidden by default)
            // ---------------------------------------------------------------
            this.txtPassword.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtPassword.LabelText = "Password";
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.TabIndex = 0;
            this.txtPassword.InputType = mtc_app.shared.presentation.components.AppInput.InputTypeEnum.Password;
            this.txtPassword.Visible = false;

            // ---------------------------------------------------------------
            // btnLogin — Primary action
            // ---------------------------------------------------------------
            this.btnLogin.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.btnLogin.Margin = new System.Windows.Forms.Padding(0, 6, 0, 4);
            this.btnLogin.Height = 44;
            this.btnLogin.Name = "btnLogin";
            this.btnLogin.TabIndex = 3;
            this.btnLogin.Text = "LOGIN";
            this.btnLogin.Type = mtc_app.shared.presentation.components.AppButton.ButtonType.Primary;
            this.btnLogin.Click += new System.EventHandler(this.btnLogin_Click);

            // ---------------------------------------------------------------
            // btnExit — Secondary action (same width as Login for consistency)
            // ---------------------------------------------------------------
            this.btnExit.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.btnExit.Margin = new System.Windows.Forms.Padding(0);
            this.btnExit.Height = 40;
            this.btnExit.Name = "btnExit";
            this.btnExit.TabIndex = 4;
            this.btnExit.Text = "Exit App";
            this.btnExit.Type = mtc_app.shared.presentation.components.AppButton.ButtonType.Outline;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);

            // ---------------------------------------------------------------
            // pnlCard — The login card container
            // ---------------------------------------------------------------
            this.pnlCard.BackColor = mtc_app.shared.presentation.styles.AppColors.CardBackground;
            this.pnlCard.Padding = new System.Windows.Forms.Padding(32, 20, 32, 20);
            this.pnlCard.Controls.Add(this.tblLayout);
            this.pnlCard.Name = "pnlCard";
            this.pnlCard.TabIndex = 0;

            // ---------------------------------------------------------------
            // LoginForm
            // ---------------------------------------------------------------
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = mtc_app.shared.presentation.styles.AppColors.Surface;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.pnlCard);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "LoginForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Login";

            this.pnlInputSwap.ResumeLayout(false);
            this.tblLayout.ResumeLayout(false);
            this.tblLayout.PerformLayout();
            this.pnlCard.ResumeLayout(false);
            this.pnlCard.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel pnlCard;
        private System.Windows.Forms.TableLayoutPanel tblLayout;
        private mtc_app.shared.presentation.components.AppLabel lblTitle;
        private mtc_app.shared.presentation.components.AppInput drpRole;
        private System.Windows.Forms.Panel pnlInputSwap;
        private mtc_app.shared.presentation.components.AppInput txtIdentity;
        private mtc_app.shared.presentation.components.AppInput txtPassword;
        private mtc_app.shared.presentation.components.AppButton btnLogin;
        private mtc_app.shared.presentation.components.AppButton btnExit;
    }
}