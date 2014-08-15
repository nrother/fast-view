using System;
using System.Collections.Generic;
using System.Text;
using PluginInterface;
using System.Windows.Forms;
using PDFLibNet;
using PDFViewer;
using System.Drawing;
using System.Reflection;

namespace FVPDFPlugin
{
    public class FVPDFPlugin : IFVPlugin
    {

        PDFWrapper doc = new PDFWrapper();
        PageViewer pageViewer1 = new PageViewer();

        public List<string> GetFileextensions()
        {
            List<string> ret = new List<string>();
            ret.Add(".pdf");
            return ret;
        }

        public void InitContainer(Panel ContentPanel, Panel ControlPanel)
        {
            ContentPanel.Controls.Add(pageViewer1);
            pageViewer1.Dock = DockStyle.Fill;
            pageViewer1.PaintMethod = PageViewer.DoubleBufferMethod.BuiltInOptimizedDoubleBuffer;
            pageViewer1.PaintControl += new PageViewer.PaintControlHandler(pageViewer1_PaintControl);
            pageViewer1.NextPage += new PageViewer.MovePageHandler(pageViewer1_NextPage);
            pageViewer1.PreviousPage += new PageViewer.MovePageHandler(pageViewer1_PreviousPage);

            IsInit = true;
        }

        bool pageViewer1_PreviousPage(object sender)
        {
            if (doc.CurrentPage > 1)
            {
                doc.PreviousPage();
                doc.RenderPage(pageViewer1.Handle, true);
                RenderFinish();
                return true;
            }
            return false;
        }

        bool pageViewer1_NextPage(object sender)
        {
            if (doc.CurrentPage < doc.PageCount)
            {
                doc.NextPage();
                doc.RenderPage(pageViewer1.Handle, true);
                RenderFinish();
                return true;
            }
            return false;
        }

        void pageViewer1_PaintControl(object sender, Rectangle view, Point location, Graphics g)
        {
            Size size = new Size(view.Right, view.Bottom);
            Rectangle rectangle = new Rectangle(location, size);
            doc.ClientBounds = rectangle;
            doc.CurrentX = view.X;
            doc.CurrentY = view.Y;
            doc.DrawPageHDC(g.GetHdc());
            g.ReleaseHdc();
        }

        public bool ShowFile(string path)
        {
            try
            {
                doc.LoadPDF(path);
                FitWidght();
                doc.RenderPage(pageViewer1.Handle, true);
                RenderFinish();
                pageViewer1.BringToFront();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            
        }

        public void DisposeHandle()
        {
            
        }

        void FitWidght()
        {
            using (PictureBox box = new PictureBox())
            {
                box.Width = pageViewer1.ClientSize.Width;
                doc.FitToWidth(box.Handle);
            }
            doc.RenderPage(pageViewer1.Handle, true);
            RenderFinish();
        }

        void RenderFinish()
        {
            pageViewer1.PageSize = new Size(doc.PageWidth, doc.PageHeight);
            pageViewer1.Invalidate();
        }

        public void ChangedWindowSize()
        {
            FitWidght();
        }

        public bool IsInit
        { get; set; }
    }
}
