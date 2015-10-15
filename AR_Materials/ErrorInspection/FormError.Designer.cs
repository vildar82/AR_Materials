namespace AR_Materials.ErrorInspection
{
   partial class FormError
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
         this.listBoxError = new System.Windows.Forms.ListBox();
         this.buttonShow = new System.Windows.Forms.Button();
         this.textBoxErr = new System.Windows.Forms.TextBox();
         this.SuspendLayout();
         // 
         // listBoxError
         // 
         this.listBoxError.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.listBoxError.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
         this.listBoxError.FormattingEnabled = true;
         this.listBoxError.ItemHeight = 18;
         this.listBoxError.Location = new System.Drawing.Point(12, 12);
         this.listBoxError.Name = "listBoxError";
         this.listBoxError.Size = new System.Drawing.Size(660, 346);
         this.listBoxError.TabIndex = 0;
         this.listBoxError.SelectedIndexChanged += new System.EventHandler(this.listBoxError_SelectedIndexChanged);
         this.listBoxError.DoubleClick += new System.EventHandler(this.listBoxError_DoubleClick);
         // 
         // buttonShow
         // 
         this.buttonShow.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
         this.buttonShow.Location = new System.Drawing.Point(12, 364);
         this.buttonShow.Name = "buttonShow";
         this.buttonShow.Size = new System.Drawing.Size(109, 30);
         this.buttonShow.TabIndex = 1;
         this.buttonShow.Text = "Показать";
         this.buttonShow.UseVisualStyleBackColor = true;
         this.buttonShow.Click += new System.EventHandler(this.buttonShow_Click);
         // 
         // textBoxErr
         // 
         this.textBoxErr.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.textBoxErr.Location = new System.Drawing.Point(12, 401);
         this.textBoxErr.Multiline = true;
         this.textBoxErr.Name = "textBoxErr";
         this.textBoxErr.ReadOnly = true;
         this.textBoxErr.Size = new System.Drawing.Size(660, 83);
         this.textBoxErr.TabIndex = 2;
         // 
         // FormError
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(689, 496);
         this.Controls.Add(this.textBoxErr);
         this.Controls.Add(this.buttonShow);
         this.Controls.Add(this.listBoxError);
         this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
         this.Margin = new System.Windows.Forms.Padding(4);
         this.Name = "FormError";
         this.Text = "Найдены ошибки";
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion

      private System.Windows.Forms.ListBox listBoxError;
      private System.Windows.Forms.Button buttonShow;
      private System.Windows.Forms.TextBox textBoxErr;
   }
}