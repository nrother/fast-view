using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using PluginInterface;
using SHDocVw;
using Shell32;
using System.Threading;
using updateSystemDotNet;
using updateSystemDotNet.appEventArgs;

namespace QuickLookWindows
{
    public partial class Form1 : Form
    {
        string curr_filename;
       
        Dictionary<string, IFVPlugin> plugins = new Dictionary<string, IFVPlugin>();
        List<IFVPlugin> needDispose = new List<IFVPlugin>();

        bool firstTime = true;
        int ExplorerHandle = -1;
        bool IsMouseDown = false;
        private Point LastCursorPosition;
        
        public Form1()
        {
            InitializeComponent();

            //Load Plugins
            Assembly assembly;
            IFVPlugin plugin;
            foreach(string file in Directory.GetFiles(Environment.CurrentDirectory + "\\plugins"))
            {
                if(!file.EndsWith(".dll"))
                    continue;
                try
                {
                    assembly = Assembly.LoadFile(file);
                    foreach(Type type in assembly.GetExportedTypes())
                    {
                        if(type.GetInterface("IFVPlugin") != null)
                        {
                            plugin = (IFVPlugin)assembly.CreateInstance(type.FullName);
                            foreach (string ext in plugin.GetFileextensions())
                            {
                                plugins.Add(ext, plugin);
                            }
                        }
                        
                    }
                }
                catch(Exception)
                {
                    continue;
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //HotKey registrieren
            SpaceWatch.Init((int)Properties.Settings.Default.FastViewKey);
            SpaceWatch.SpacePressed += new EventHandler(SpacePressed);

            //Fenster abrunden
            Region = GetRoundedRect(new RectangleF(0, 0, ClientRectangle.Width, ClientRectangle.Height), 20);

            if(Properties.Settings.Default.UpdateEnabled)
                updateController1.checkForUpdatesAsync();
        }


        private void SpacePressed(object sender, EventArgs e)
        {
            if (Visible)
            {
                //den eingbe fokus auf das panel verschieben! WICHTIG!!!
                panel1.Focus();
                //fenster wieder auf normal setzen
                SetMaximized(false);
                //ausblenden
                Visible = false;
                //Plugins anweisen die Handles freizugeben
                DisposeHandles();
                //timer ausschalten
                timer1.Enabled = false;
            }
            else
            {
                curr_filename = GetExplorerPath();
                if (!String.IsNullOrEmpty(curr_filename))
                {
                    SetDisplay(curr_filename);
                    timer1.Enabled = true;
                }
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetForegroundWindow();

        private string GetExplorerPath()
        {
            try
            {
                ShellWindows shellWindows = new ShellWindowsClass();
                int foregroundHandle = GetForegroundWindow().ToInt32();
                ExplorerHandle = foregroundHandle;

                foreach (InternetExplorer ie in shellWindows)
                {
                    if (ie.HWND == foregroundHandle)
                    {
                        return ((IShellFolderViewDual2)ie.Document).FocusedItem.Path;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
            return null;
        }

        private void DisposeHandles()
        {
            foreach (IFVPlugin plugin in needDispose)
               plugin.DisposeHandle();
        }

        private void SetDisplay(string path)
        {
            label1.Text = Path.GetFileName(path);
            string ext = Path.GetExtension(path).ToLower();
            bool PluginFound = false;
            try
             {
                //Plugins
                 if (plugins.ContainsKey(ext))
                 {
                     if (!plugins[ext].IsInit)
                         plugins[ext].InitContainer(panel2, panel1);
                     if (!plugins[ext].ShowFile(path))
                         throw new Exception();
                     PluginFound = true;
                     needDispose.Add(plugins[ext]);
                 }

                 if (!PluginFound)
                 {
                     if (String.IsNullOrEmpty(Path.GetExtension(path).ToLower()))
                     {
                         //Ordner
                         //Plugin, das Ordner überschreibt suchen..
                         if (plugins.ContainsKey("dir"))
                         {
                             if (!plugins["dir"].IsInit)
                                 plugins["dir"].InitContainer(panel2, panel1);
                             if (!plugins["dir"].ShowFile(path))
                                 throw new Exception();
                             PluginFound = true;
                             needDispose.Add(plugins[ext]);
                         }
                         else //Standart für Ordner
                         {

                             richTextBox1.Text = "Ordner:\n\n";
                             string[] dirs = Directory.GetDirectories(path);
                             foreach (string dir in dirs)
                                 richTextBox1.Text += dir + "\n";
                             richTextBox1.Text += "\nDateien:\n\n";
                             dirs = Directory.GetFiles(path);
                             foreach (string dir in dirs)
                                 richTextBox1.Text += dir + "\n";

                             richTextBox1.BringToFront();
                         }
                     }
                     else //unbekannte Datei
                     {
                         //plugin suchen für Default
                         if (plugins.ContainsKey("default"))
                         {
                             if (!plugins["default"].IsInit)
                                 plugins["default"].InitContainer(panel2, panel1);
                             if (!plugins["default"].ShowFile(path))
                                 throw new Exception();
                             PluginFound = true;
                             needDispose.Add(plugins[ext]);
                         }
                         else
                         {
                             richTextBox1.Text = "Unbekannter Dateityp!";
                             richTextBox1.BringToFront();
                         }
                     }
                 }

                //fenster sichtbar machen
                Visible = true;
            }
            catch (Exception)
            {
                richTextBox1.Text = "Die Datei kann nicht geöffnet werden!";
                richTextBox1.BringToFront();
            }
        }

        private Region GetRoundedRect(RectangleF BaseRect, int Radius)
        {
            // If corner radius is less than or equal to zero, return the original rectangle
            if (Radius <= 0) { return new Region(BaseRect); }
            // If corner radius is greater than or equal to half the width or height (whichever is shorter) then
            //return a capsule instead of a lozenge.
            if (Radius >= (Math.Min(BaseRect.Width, BaseRect.Height) / 2.0))
                return GetCapsule(BaseRect);

            float Diameter = Radius + Radius;
            RectangleF ArcRect = new RectangleF(BaseRect.Location, new SizeF(Diameter, Diameter));
            GraphicsPath RR = new GraphicsPath();
            // top left arc
            RR.AddArc(ArcRect, 180, 90);
            // top right arc
            ArcRect.X = BaseRect.Right - Diameter;
            RR.AddArc(ArcRect, 270, 90);
            // bottom right arc
            ArcRect.Y = BaseRect.Bottom - Diameter;
            RR.AddArc(ArcRect, 0, 90);
            // bottom left arc
            ArcRect.X = BaseRect.Left;
            RR.AddArc(ArcRect, 90, 90);
            RR.CloseFigure();

            return new Region(RR);
        }

        private Region GetCapsule(RectangleF BaseRect)
        {
            float Diameter;
            RectangleF ArcRect;
            GraphicsPath RR = new GraphicsPath();

            try
            {
                if (BaseRect.Width > BaseRect.Height)
                {
                    //return Horizonal capsule
                    Diameter = BaseRect.Height;
                    ArcRect = new RectangleF(BaseRect.Location, new SizeF(Diameter, Diameter));
                    RR.AddArc(ArcRect, 90, 180);
                    ArcRect.X = BaseRect.Right - Diameter;
                    RR.AddArc(ArcRect, 270, 180);

                }
                else if (BaseRect.Height > BaseRect.Width)
                {
                    // Return vertical capsule
                    Diameter = BaseRect.Width;
                    ArcRect = new RectangleF(BaseRect.Location, new SizeF(Diameter, Diameter));
                    RR.AddArc(ArcRect, 180, 180);
                    ArcRect.Y = BaseRect.Bottom - Diameter;
                    RR.AddArc(ArcRect, 0, 180);
                }
                else
                {
                    //return circle
                    RR.AddEllipse(BaseRect);
                }
            }
            catch (Exception e)
            {
                string sLastError = e.Message;
                RR.AddEllipse(BaseRect);
            }
            finally
            {
                RR.CloseFigure();
            }
            return new Region(RR);
        }

        private void SetMaximized(bool mazimized)
        {
            if (mazimized)
            {
                WindowState = FormWindowState.Maximized;
                Region = null;
            }
            else
            {
                WindowState = FormWindowState.Normal;
                Region = GetRoundedRect(new RectangleF(0, 0, ClientRectangle.Width, ClientRectangle.Height), 20);
            }

            //Plugins benachrichtigen
            foreach (IFVPlugin plugin in needDispose)
                plugin.ChangedWindowSize();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            SpaceWatch.RealeseHook();
            Properties.Settings.Default.Save();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(curr_filename))
                Process.Start(curr_filename);
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            //Wenn Normal, dann mazimized = true ...
            SetMaximized(WindowState == FormWindowState.Normal);
        }

        private void beendenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DisposeHandles();
            Application.Exit();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            if (firstTime)
            {
                Hide();
                firstTime = true;
            }
        }

        private void updateController1_updateFound(object sender, updateFoundEventArgs e)
        {
            if (updateController1.showUpdateDialog() == DialogResult.OK)
            {
                updateController1.downloadUpdatesDialog();
                updateController1.applyUpdate();
            }
            
        }

        private void einstellungenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SettingsForm sf = new SettingsForm();
            if (sf.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.FastViewKey = sf.OpenKey;
                Properties.Settings.Default.UpdateEnabled = sf.checkBox1.Checked;

                MessageBox.Show("FastView muss neugestartet werden, um die Änderungen zu übernehmen.", "FastView", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //check, if new file in explorer selectet
            string new_filename = GetExplorerPath();
            if (new_filename != curr_filename && !String.IsNullOrEmpty(new_filename))
            {
                curr_filename = new_filename;
                DisposeHandles();
                SetDisplay(new_filename);
            }
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            IsMouseDown = true;
            LastCursorPosition = new Point(e.X, e.Y);
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsMouseDown)
            {
                this.Location = new Point(this.Left - (this.LastCursorPosition.X - e.X), this.Top - (this.LastCursorPosition.Y - e.Y));
                this.Invalidate();
            }
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            IsMouseDown = false;
        }
    }
}
