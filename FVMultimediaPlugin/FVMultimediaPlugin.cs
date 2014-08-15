using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PluginInterface;
using System.Windows.Forms;
using System.IO;
using IrrKlang;
using QuartzTypeLib;
using System.Drawing;
using System.Reflection;
using EConTech.Windows.MACUI;

namespace FVMultimediaPlugin
{
    enum MediaType { Audio, Video }

    public class FVMultimediaPlugin : IFVPlugin
    {
        PictureBox pictureBox1 = new PictureBox();
        Label label1 = new Label();
        Panel panel1 = new Panel();
        PictureBox pictureBox2 = new PictureBox();
        MACTrackBar trackBar1 = new MACTrackBar();
        Timer timer1 = new Timer();
        TableLayoutPanel tlp = new TableLayoutPanel();

        string filepath;
        MediaType mediaType;

        //DirectShow
        private const int WM_APP = 0x8000;
        private const int WM_GRAPHNOTIFY = WM_APP + 1;
        private const int EC_COMPLETE = 0x01;
        private const int WS_CHILD = 0x40000000;
        private const int WS_CLIPCHILDREN = 0x2000000;

        private FilgraphManager FilterGraph = null;
        private IBasicAudio BasicAudio = null;
        private IVideoWindow VideoWindow = null;
        private IMediaEvent MediaEvent = null;
        private IMediaEventEx MediaEventEx = null;
        private IMediaPosition MediaPosition = null;
        private IMediaControl MediaControl = null;

        //IrrKlang
        ISoundEngine soundengine = new ISoundEngine();
        ISound currentSound;

        public void ChangedWindowSize()
        {
            if (VideoWindow != null)
            {
                VideoWindow.SetWindowPosition(panel1.ClientRectangle.Left,
                            panel1.ClientRectangle.Top,
                            panel1.ClientRectangle.Width,
                            panel1.ClientRectangle.Height);
            }
        }

        public void DisposeHandle()
        {
            if (MediaControl != null)
                MediaControl.Stop();
            timer1.Stop();
            soundengine.StopAllSounds();
            if (pictureBox1.Image != null)
                pictureBox1.Image.Dispose();
            CleanUp();
            if (currentSound != null)
                currentSound.Dispose();
            tlp.Visible = false;
        }

        public List<string> GetFileextensions()
        {
            List<string> ret = new List<string>();

            ret.Add(".ico");
            ret.Add(".bmp");
            ret.Add(".jpeg");
            ret.Add(".jpg");
            ret.Add(".gif");
            ret.Add(".png");
            ret.Add(".dib");
            ret.Add(".jpe");
            ret.Add(".jfif");
            ret.Add(".tif");
            ret.Add(".tiff");
            ret.Add(".avi");
            ret.Add(".wmv");
            ret.Add(".mpeg");
            ret.Add(".wav");
            ret.Add(".ogg");
            ret.Add(".mod");
            ret.Add(".it");
            ret.Add(".s3d");
            ret.Add(".xm");
            ret.Add(".mp3");

            return ret;
        }

        public void InitContainer(Panel ContentPanel, Panel ControlPanel)
        {   
            ContentPanel.Controls.Add(pictureBox1);
            ContentPanel.Controls.Add(label1);
            ContentPanel.Controls.Add(panel1);

            pictureBox1.Dock = DockStyle.Fill;
            label1.Dock = DockStyle.Fill;
            panel1.Dock = DockStyle.Fill;
            

            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15.0f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 85.0f));           

            trackBar1.Scroll += new EventHandler(trackBar1_Scroll);
            trackBar1.TickStyle = TickStyle.None;
            trackBar1.TextTickStyle = TickStyle.None;
            trackBar1.TrackLineHeight = 16;
            trackBar1.TrackerColor = Color.Black;
            trackBar1.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            tlp.Controls.Add(trackBar1, 1, 0);

            SetPictureBoxPause(true);
            pictureBox2.Size = new Size(1, 1); //belibige größe zuweisen, warum auch immer...
            pictureBox2.Click += new EventHandler(pictureBox2_Click);
            pictureBox2.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox2.Dock = DockStyle.Fill;
            tlp.Controls.Add(pictureBox2, 0, 0);      

            tlp.Visible = false;
            tlp.Dock = DockStyle.Fill;
            ControlPanel.Controls.Add(tlp);

            timer1.Interval = 100;
            timer1.Tick += new EventHandler(timer1_Tick);

            IsInit = true;

        }

        bool GetPictureBoxImageIsPlay()
        {
            return (bool)pictureBox2.Image.Tag;
        }

        void pictureBox2_Click(object sender, EventArgs e)
        {
            if (GetPictureBoxImageIsPlay())
            {
                SetPictureBoxPause(true);
                if (mediaType == MediaType.Video)
                    MediaControl.Run();
                else if (mediaType == MediaType.Audio)
                {
                    currentSound.Paused = false;
                    if (currentSound.Finished)
                    {
                        currentSound = soundengine.Play2D(filepath);
                        timer1.Start();
                    }
                }
            }
            else
            {
                SetPictureBoxPause(false);
                if (mediaType == MediaType.Video)
                    MediaControl.Pause();
                else if (mediaType == MediaType.Audio)
                    currentSound.Paused = true;
            }
        }

        void SetPictureBoxPause(bool pauseOrPlay)
        {
            if (pauseOrPlay)
            {
                pictureBox2.Image = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream("FVMultimediaPlugin.Resources.fastview_pause.png"));
                pictureBox2.Image.Tag = false;
            }
            else
            {
                pictureBox2.Image = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream("FVMultimediaPlugin.Resources.fastview_play.png"));
                pictureBox2.Image.Tag = true;
            }
        }

        void timer1_Tick(object sender, EventArgs e)
        {
            UpdateSlider();
        }

        private void UpdateSlider()
        {
            if (MediaPosition != null && mediaType == MediaType.Video)
            {
                trackBar1.Maximum = (int)MediaPosition.Duration;
                trackBar1.Value = (int)MediaPosition.CurrentPosition;
                if (MediaPosition.Duration == MediaPosition.CurrentPosition)
                {
                    //Video fertig
                    MediaControl.Stop();
                    MediaPosition.CurrentPosition = 0;
                    SetPictureBoxPause(false);
                }
            }
            else if (mediaType == MediaType.Audio)
            {
                trackBar1.Maximum = (int)(currentSound.PlayLength / 1000);
                trackBar1.Value = (int)(currentSound.PlayPosition / 1000);
                if (currentSound.Finished)
                {
                    //sollte eig. nicht in UpdateSlider sein, aber ist so einfacher
                    currentSound.Stop();
                    currentSound.PlayPosition = 0;
                    trackBar1.Value = 0;
                    timer1.Stop();
                    SetPictureBoxPause(false);
                }
            }
            else
            {
                trackBar1.Maximum = 1;
                trackBar1.Value = 0;
            }
        }

        void trackBar1_Scroll(object sender, EventArgs e)
        {
            if (mediaType == MediaType.Video)
            {
                MediaControl.Pause();
                MediaPosition.CurrentPosition = trackBar1.Value;
                if (!GetPictureBoxImageIsPlay())
                    MediaControl.Run();
            }
            else if (mediaType == MediaType.Audio)
            {
                currentSound.Paused = true;
                currentSound.PlayPosition = (uint)trackBar1.Value * 1000;
                if (!GetPictureBoxImageIsPlay())
                    currentSound.Paused = false;
            }
        }

        public bool ShowFile(string path)
        {
            filepath = path;
            try
            {
                switch (Path.GetExtension(path).ToLower())
                {
                    case ".ico":
                        //Icon
                        pictureBox1.Image = System.Drawing.Icon.ExtractAssociatedIcon(path).ToBitmap();
                        pictureBox1.SizeMode = PictureBoxSizeMode.CenterImage;
                        pictureBox1.BringToFront();
                        break;
                    case ".bmp":
                    case ".jpeg":
                    case ".jpg":
                    case ".gif":
                    case ".png":
                    case ".dib":
                    case ".jpe":
                    case ".jfif":
                    case ".tif":
                    case ".tiff":
                        //Bild
                        Image image = Image.FromFile(path);
                        if (image.Size.Height < pictureBox1.Size.Height && image.Size.Width < pictureBox1.Size.Width)
                            pictureBox1.SizeMode = PictureBoxSizeMode.CenterImage;
                        else
                            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                        pictureBox1.Image = image;
                        pictureBox1.BringToFront();
                        break;
                    case ".avi":
                    case ".wmv":
                    case ".mpeg":
                        //Video
                        mediaType = MediaType.Video;
                        if (!PlayVideo(path))
                            throw new Exception();
                        timer1.Start();
                        tlp.Visible = true;
                        SetPictureBoxPause(true);
                        panel1.BringToFront();
                        break;
                    case ".wav":
                    case ".ogg":
                    case ".mod":
                    case ".it":
                    case ".s3d":
                    case ".xm":
                        //Music
                        mediaType = MediaType.Audio;
                        currentSound = soundengine.Play2D(path);
                        timer1.Start();
                        tlp.Visible = true;
                        SetPictureBoxPause(true);
                        panel1.BringToFront();
                        break;
                    case ".mp3":
                        //MP3s
                        if (File.Exists("ikpMP3.dll"))
                        {
                            mediaType = MediaType.Audio;
                            currentSound = soundengine.Play2D(path);
                            timer1.Start();
                            tlp.Visible = true;
                            SetPictureBoxPause(true);
                            panel1.BringToFront();
                        }
                        else
                        {
                            label1.Text = "MP3 Dateien können nur abgespielt werden, wenn sich die Datei \"ikpMP3.dll\" im gleichen Ordner wie die Dateien dieses Plugins befindet.\nDiese Datei kann ich aus Lizenzgründen nicht mit zum Download anbieten, sie ist aber auf der Website http://www.ambiera.com/irrklang/downloads.html im ersten Download enthalten.\nDie Datei muss einfach in das gleiche Verzeichniss wie alle anderen kopiert werden!";
                            label1.BringToFront();
                        }
                        break;
                }
                
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool PlayVideo(string path)
        {
            CleanUp();

            FilterGraph = new FilgraphManager();
            FilterGraph.RenderFile(path);

            BasicAudio = FilterGraph as IBasicAudio;

            try
            {
                VideoWindow = FilterGraph as IVideoWindow;
                VideoWindow.Owner = (int)panel1.Handle;
                VideoWindow.WindowStyle = WS_CHILD | WS_CLIPCHILDREN;
                VideoWindow.SetWindowPosition(panel1.ClientRectangle.Left,
                        panel1.ClientRectangle.Top,
                        panel1.ClientRectangle.Width,
                        panel1.ClientRectangle.Height);
            }
            catch (Exception)
            {
                VideoWindow = null;
                return false;
            }

            MediaEvent = FilterGraph as IMediaEvent;
            MediaEventEx = FilterGraph as IMediaEventEx;

            MediaPosition = FilterGraph as IMediaPosition;
            MediaControl = FilterGraph as IMediaControl;

            MediaControl.Run();
            return true;
        }

        //setzt alle DirectShow Sachen auf null
        private void CleanUp()
        {
            if (MediaControl != null)
                MediaControl.Stop();

            if (MediaEventEx != null)
                MediaEventEx.SetNotifyWindow(0, 0, 0);

            if (VideoWindow != null)
            {
                VideoWindow.Visible = 0;
                VideoWindow.Owner = 0;
            }

            if (MediaControl != null) MediaControl = null;
            if (MediaPosition != null) MediaPosition = null;
            if (MediaEventEx != null) MediaEventEx = null;
            if (MediaEvent != null) MediaEvent = null;
            if (VideoWindow != null) VideoWindow = null;
            if (BasicAudio != null) BasicAudio = null;
            if (FilterGraph != null) FilterGraph = null;
        }

        public bool IsInit
        { get; set; }
    }
}
