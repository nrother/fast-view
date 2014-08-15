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
    public partial class SettingsForm : Form
    {
        public Keys OpenKey;
        
        public SettingsForm()
        {
            InitializeComponent();
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            //Einstellungen laden..
            checkBox1.Checked = Properties.Settings.Default.UpdateEnabled;

            label2.Text = Properties.Settings.Default.FastViewKey.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SetKeyForm skf = new SetKeyForm();
            skf.ShowDialog();
            OpenKey = skf.PressedKey;

            label2.Text = OpenKey.ToString();
        }
    }
}
