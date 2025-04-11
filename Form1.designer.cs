namespace Soundpad
{
    partial class Form1
    {
        /// <summary>
        /// Limpeza de recursos.
        /// </summary>
        /// <param name="disposing">true se gerenciado; caso contrário, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        private System.ComponentModel.IContainer components = null;

        #region Código gerado pelo Designer

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(400, 600);
            this.Name = "Form1";
            this.Text = "Soundpad";
            this.ResumeLayout(false);
        }

        #endregion
    }
}
