using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuickLookWindows
{
    public partial class SetKeyForm : Form
    {
        public Keys PressedKey;
        
        public SetKeyForm()
        {
            InitializeComponent();
        }

        private void SetKeyForm_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            PressedKey = e.KeyCode;
            Close();
        }
    }
}
