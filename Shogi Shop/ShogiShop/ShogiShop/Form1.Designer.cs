namespace ShogiShop
{
    partial class ShogiForm
    {
        private System.ComponentModel.IContainer? components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Text = "Shogi";
            this.Paint += ShogiForm_Paint;
            this.MouseClick += ShogiForm_MouseClick;
        }
    }
}