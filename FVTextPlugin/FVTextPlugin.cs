using System;
using System.Collections.Generic;
using System.Text;
using PluginInterface;
using System.IO;
using System.Windows.Forms;
using System.Drawing;

namespace FVTextPlugin
{
    public class FVTextPlugin : IFVPlugin
    {
        StreamReader reader;

        TextBox textBox1 = new TextBox();
        
        public List<string> GetFileextensions()
        {
            StreamReader sr = new StreamReader("text_ext.txt");
            List<string> ret = new List<string>();

            while (sr.Peek() != -1)
                ret.Add(sr.ReadLine());

            return ret;
        }

        public void InitContainer(Panel ContentPanel, Panel ControlPanel)
        {
            ContentPanel.Controls.Add(textBox1);

            textBox1.Dock = DockStyle.Fill;
            textBox1.Multiline = true;
            textBox1.ReadOnly = true;
            textBox1.AutoSize = true;
            textBox1.ScrollBars = ScrollBars.Both;

            IsInit = true;
        }

        public bool ShowFile(string path)
        {
            try
            {
                reader = new StreamReader(path);
                textBox1.Text = reader.ReadToEnd();
                textBox1.BringToFront();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void DisposeHandle()
        {
            if (reader != null)
            {
                reader.Close();
                reader.Dispose();
                reader = null;
            }
        }

        public void ChangedWindowSize()
        { }

        public bool IsInit
        { get; set; }
    }
}
