using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PluginInterface;
using System.Windows.Forms;
using System.Reflection;
using Office = Microsoft.Office.Interop;
using System.Threading;

namespace FVOfficePlugin
{
    public class FVOfficePlugin : IFVPlugin
    {
        RichTextBox rtfBox = new RichTextBox();

        string curr_clpbrd_content;

        public void ChangedWindowSize()
        {
            rtfBox.Dock = DockStyle.Fill;
        }

        public void DisposeHandle()
        { }

        public List<string> GetFileextensions()
        {
            List<string> ret = new List<string>();

            ret.Add(".doc");
            ret.Add(".docx");

            return ret;
        }

        public void InitContainer(Panel ContentPanel, Panel ControlPanel)
        {
            ContentPanel.Controls.Add(rtfBox);

            rtfBox.Dock = DockStyle.Fill;
            rtfBox.ReadOnly = true;

            IsInit = true;
        }

        public bool IsInit
        { get; set; }

        public bool ShowFile(string path)
        {
            try
            {
                rtfBox.Text = GetTextFromWordFile(path);
                rtfBox.BringToFront();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        string GetTextFromWordFile(string path)
        {            
            Office.Word._Application appWord = null;
            Office.Word._Document docWord = null;

            Object filename = path;
            Object ConfirmConversions = Missing.Value;
            Object ReadOnly = true;
            Object AddToRecentFiles = Missing.Value;
            Object PasswordDocument = Missing.Value;
            Object PasswordTemplate = Missing.Value;
            Object Revert = Missing.Value;
            Object WritePasswordDocument = Missing.Value;
            Object WritePasswordTemplate = Missing.Value;
            Object Format = Missing.Value;
            Object Encoding = Missing.Value;
            Object Visible = false;

            Object saveChanges = false;
            Object originalFormat = null;
            Object routeDoc = null;

            appWord = new Office.Word.Application();
            appWord.Visible = false;
            docWord = appWord.Documents.Open2000(ref filename, ref ConfirmConversions, ref ReadOnly,
                              ref AddToRecentFiles, ref PasswordDocument, ref PasswordTemplate,
                              ref Revert, ref WritePasswordDocument, ref WritePasswordTemplate,
                              ref Format, ref Encoding, ref Visible);
            if (docWord.ProtectionType == Office.Word.WdProtectionType.wdNoProtection)
            {
                docWord.ActiveWindow.Selection.WholeStory();
                docWord.ActiveWindow.Selection.Copy();
                GetClipboardContent();
            }
            else
            {
                curr_clpbrd_content = "";
            }
            appWord.ActiveDocument.Close(ref saveChanges, ref originalFormat, ref routeDoc);
            appWord.Quit(ref saveChanges, ref originalFormat, ref routeDoc);
            

            return curr_clpbrd_content;
        }

        //kann auch aus mta aufgrefufen werden
        void GetClipboardContent()
        {
            Thread thread = new Thread(new ThreadStart(ClpbrdHelper));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        void ClpbrdHelper()
        {
            IDataObject helper = Clipboard.GetDataObject();
            curr_clpbrd_content = helper.GetData(DataFormats.Text).ToString();
        }


    }
}
