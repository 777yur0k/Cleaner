using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Collections.Generic;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Cleaner
{
    public partial class MainWindow : Window
    {
        static List<CheckBox> CheckBoxes = new();
        static Grid Grid1;
        List<string> Files = new();
        List<string> Directories = new();
        BackgroundWorker backgroundWorker = new();
        static int filesCount;
        static SHQUERYRBINFO sqrbi = new();

        public MainWindow()
        {
            InitializeComponent();
            Grid1 = OptionsGrid;
            if (CheckRecycleBin()) CheckBoxes.Add(new CheckBox {Content = "Recycle Bin", Name = "RecycleBin", IsChecked = true});
            if (CheckDerictories(Path.GetTempPath()) || CheckDerictories(Environment.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.Machine)) || CheckDerictories("C:/Users/" + Environment.UserName + "/AppData/Local/Temp"))
                CheckBoxes.Add(new CheckBox {Content = "Temp Files", Name = "TempFiles", IsChecked = true});

            for (var i = 0; i < CheckBoxes.Count; i++)
            {
                Grid1.Children.Add(CheckBoxes[i]);
                Grid.SetRow(CheckBoxes[i], i);
            }
        }

        void DoWork(object sender, DoWorkEventArgs e)
        {
            for (var i = 0; i < Directories.Count; i++)
            {
                try
                {
                    Directory.Delete(Directories[i], true);
                }
                catch{}
                backgroundWorker.ReportProgress(i);
            }

            for (var i = 0; i < Files.Count; i++)
            {
                ulong size = 0;
                try
                {
                    size = (ulong)new FileInfo(Files[i]).Length;
                    File.Delete(Files[i]);
                }
                catch{}
                finally
                {
                    AddSize(size);
                    filesCount++;
                }
                backgroundWorker.ReportProgress(i);
            }
        }

        void Button_Click(object sender, RoutedEventArgs e)
        {
            Button.IsEnabled = false;

            if (CheckBoxes[FindCheckBox("RecycleBin")].IsChecked == true)
            {
                AddSize(sqrbi.i64Size);
                filesCount += (int)sqrbi.i64NumItems;
                SHEmptyRecycleBin(IntPtr.Zero, null, 0 | 0 | 0);
                ProgressBar.Value++;
            }
            
            if(CheckBoxes[FindCheckBox("TempFiles")].IsChecked == true)
            {
                AddFilesToClear(Path.GetTempPath());
                AddFilesToClear(Environment.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.Machine));
                if (Directory.Exists("C:/Users/" + Environment.UserName + "/AppData/Local/Temp"))
                    if (Path.GetTempPath() != "C:/Users/" + Environment.UserName + "/AppData/Local/Temp") AddFilesToClear("C:/Users/" + Environment.UserName + "/AppData/Local/Temp");

                ProgressBar.Maximum += Files.Count + Directories.Count;
            }

            backgroundWorker.DoWork += DoWork;
            backgroundWorker.RunWorkerCompleted += RunWorkerCompleted;
            backgroundWorker.ProgressChanged += ProgressChanged;
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.RunWorkerAsync();

            SetResultText();
        }

        int FindCheckBox(string name)
        {
            for (var i = 0; i < CheckBoxes.Count; i++) if (CheckBoxes[i].Name == name) return i;
            return 0;
        }

        void RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) => SetResultText();

        void ProgressChanged(object sender, ProgressChangedEventArgs e) => ProgressBar.Value++;

        bool CheckDerictories(string Path)
        {
            if (!Directory.Exists(Path)) return false;

            foreach (string f in Directory.EnumerateFiles(Path)) return true;
            foreach (string d in Directory.GetDirectories(Path)) return true;
            return false;
        }

        void AddFilesToClear(string Path)
        {
            foreach (string FilePath in Directory.EnumerateFiles(Path)) Files.Add(FilePath);
            foreach (string DirectoryPath in Directory.EnumerateDirectories(Path)) Directories.Add(DirectoryPath);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SHQUERYRBINFO
        {
            public int cbSize;
            public ulong i64Size;
            public ulong i64NumItems;
        }

        public static class SizeInfo
        {
            public static ulong b;
            public static ulong kb;
            public static ulong mb;
            public static ulong gb;
        }

        void SetResultText()
        {
            var strName = "";

            if (SizeInfo.gb > 0) strName = SizeInfo.gb + " gb cleared";
            else if (SizeInfo.mb > 0) strName = SizeInfo.mb + " mb cleared";
            else if (SizeInfo.kb > 0) strName = SizeInfo.kb + " kb cleared";
            else strName = SizeInfo.b + " b cleared";

            TextBlock.Text = filesCount + " files deleted " + strName;
        }

        static void AddSize(ulong size)
        {
            var i = 0;
            for (i = 0; size > 1024; i++) size /= 1024;

            switch (i)
            {
                case 0:
                    SizeInfo.b += size;
                    break;
                case 1:
                    SizeInfo.kb += size;
                    break;
                case 2:
                    SizeInfo.mb += size;
                    break;
                case 3:
                    SizeInfo.gb += size;
                    break;
            }
        }

        static bool CheckRecycleBin()
        {
            sqrbi.cbSize = Marshal.SizeOf(typeof(SHQUERYRBINFO));
            var v = SHQueryRecycleBin(string.Empty, ref sqrbi);
            if (sqrbi.i64NumItems > 0) return true;
            else return false;
        }

        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)] static extern int SHEmptyRecycleBin(IntPtr hWnd, string pszRootPath, int dwFlags);
        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)] static extern int SHQueryRecycleBin(string pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);
    }
}