using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PluginInterface
{
    public interface IFVPlugin
    {
        List<string> GetFileextensions();
        void InitContainer(Panel ContentPanel, Panel ControlPanel);
        bool ShowFile(string path);
        void DisposeHandle();
        void ChangedWindowSize();
        bool IsInit { get; set; }
    }
}
