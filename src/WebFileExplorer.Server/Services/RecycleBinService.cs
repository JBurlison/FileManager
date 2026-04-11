namespace WebFileExplorer.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using Windows.Win32;
    using Windows.Win32.Foundation;
    using WebFileExplorer.Shared.Models;

    public interface IRecycleBinService
    {
        bool IsSupported { get; }
        IEnumerable<RecycleBinItem> GetDeletedItems();
        bool RestoreItem(string id);
        bool DeleteItem(string id);
        bool EmptyBin();
        bool MoveToRecycleBin(string path);
    }
    public interface IWindowsShellService : IRecycleBinService {}
    public class RecycleBinService : IRecycleBinService {
        private readonly IWindowsShellService s;
        public RecycleBinService(IWindowsShellService s) => this.s = s;
        public bool IsSupported => s.IsSupported;
        public IEnumerable<RecycleBinItem> GetDeletedItems() => s.GetDeletedItems();
        public bool RestoreItem(string id) => s.RestoreItem(id);
        public bool DeleteItem(string id) => s.DeleteItem(id);
        public bool EmptyBin() => s.EmptyBin();
        public bool MoveToRecycleBin(string path) => s.MoveToRecycleBin(path);
    }
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class WindowsShellService : IWindowsShellService {
        public bool IsSupported 
        {
            get 
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return false;
                try 
                {
                    Type? shellAppType = Type.GetTypeFromProgID("Shell.Application");
                    return shellAppType != null;
                }
                catch { return false; }
            }
        }

        public IEnumerable<RecycleBinItem> GetDeletedItems() 
        {
            var res = new List<RecycleBinItem>();
            try 
            {
                Type? shellAppType = Type.GetTypeFromProgID("Shell.Application");
                if (shellAppType != null) {
                    dynamic? shell = Activator.CreateInstance(shellAppType);
                    dynamic folder = shell?.NameSpace(10); // ssfBITBUCKET
                    if (folder != null) {
                        foreach (dynamic item in folder.Items()) {
                            try {
                                string name = item.Name;
                                string id = item.Path;
                                string orig = folder.GetDetailsOf(item, 1) ?? ""; // Original Location
                                string dateStr = folder.GetDetailsOf(item, 2); // Date Deleted
                                long size = 0;
                                try { size = (long)item.Size; } catch {}
                                DateTimeOffset delDate = DateTimeOffset.Now;
                                if (!string.IsNullOrEmpty(dateStr) && DateTimeOffset.TryParse(dateStr, out var d)) {
                                    delDate = d;
                                }
                                res.Add(new RecycleBinItem(name, orig, delDate, id, size));
                            } catch { } // skip individual faults
                        }
                    }
                }
            } catch { }
            return res;
        }
        
        public bool RestoreItem(string id) 
        {
             try {
                Type? shellAppType = Type.GetTypeFromProgID("Shell.Application");
                if (shellAppType != null) {
                    dynamic? shell = Activator.CreateInstance(shellAppType);
                    dynamic folder = shell?.NameSpace(10);
                    if (folder != null) {
                        foreach (dynamic item in folder.Items()) {
                            if (item.Path == id) {
                                foreach(dynamic verb in item.Verbs()) {
                                    string v = verb.Name.Replace("&", "");
                                    if (v.Equals("Restore", StringComparison.OrdinalIgnoreCase)) {
                                        verb.DoIt();
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            } catch { }
            return false;
        }

        public bool DeleteItem(string id) 
        {
             try {
                Type? shellAppType = Type.GetTypeFromProgID("Shell.Application");
                if (shellAppType != null) {
                    dynamic? shell = Activator.CreateInstance(shellAppType);
                    dynamic folder = shell?.NameSpace(10);
                    if (folder != null) {
                        foreach (dynamic item in folder.Items()) {
                            if (item.Path == id) {
                                foreach(dynamic verb in item.Verbs()) {
                                    string v = verb.Name.Replace("&", "");
                                    if (v.Equals("Delete", StringComparison.OrdinalIgnoreCase)) {
                                        verb.DoIt();
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            } catch { }
            return false;
        }

        public bool EmptyBin() 
        {
            try {
                // SHERB_NOCONFIRMATION = 1, SHERB_NOPROGRESSUI = 2, SHERB_NOSOUND = 4
                uint flags = 1 | 2 | 4; 
                int res = PInvoke.SHEmptyRecycleBin(new HWND(IntPtr.Zero), (string?)null, flags);
                return res == 0;
            } catch { return false; }
        }

        public bool MoveToRecycleBin(string path) 
        {
            try {
                if (!System.IO.File.Exists(path) && !System.IO.Directory.Exists(path)) return false;
                
                var fo = new Windows.Win32.UI.Shell.SHFILEOPSTRUCTW {
                    wFunc = 3, // FO_DELETE
                    fFlags = (ushort)(0x40 | 0x10 | 0x04 | 0x0400) // ALLOWUNDO | NOCONFIRMATION | SILENT | NOERRORUI
                };
                unsafe {
                    fixed (char* pFrom = path + '\0' + '\0')
                    {
                        fo.pFrom = (Windows.Win32.Foundation.PCZZWSTR)pFrom;
                        int result = PInvoke.SHFileOperation(ref fo);
                        return result == 0;
                    }
                }
            } catch { return false; }
        }
    }
}
