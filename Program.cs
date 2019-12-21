using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileBackupService
{
    class Program
    {
        private static string _sourceDirName;
        private static string _destinationDirName;

        static void Main(string[] args)
        {
            InitializeComponent();
            WhatchForFileChange();
        }

        static void InitializeComponent()
        {
            _sourceDirName = @"" + ConfigurationManager.AppSettings.Get("SourceFile");
            _destinationDirName = @"" + ConfigurationManager.AppSettings.Get("DestinationFile");
        }

        private static void WhatchForFileChange()
        {
            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                watcher.Path = _sourceDirName;
                watcher.NotifyFilter = NotifyFilters.LastAccess
                                     | NotifyFilters.LastWrite
                                     | NotifyFilters.FileName
                                     | NotifyFilters.DirectoryName;
                watcher.Changed += OnChanged;
                watcher.Created += OnChanged;
                watcher.Deleted += OnChanged;
                watcher.Renamed += OnChanged;
                watcher.EnableRaisingEvents = true;

                while (true)
                {
                    Thread.Sleep(5000);
                }
            }
        }

        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            Thread.Sleep(1000);
            DirectoryCopy(_sourceDirName, _destinationDirName, true);
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs = true)
        {
            DirectoryInfo sourceDir = new DirectoryInfo(sourceDirName);
            if (!sourceDir.Exists) throw new ArgumentNullException("Specified source directory do not exist");
            if (!Directory.Exists(destDirName)) Directory.CreateDirectory(destDirName);

            FileInfo[] files = sourceDir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, true);
                Console.WriteLine("Copying file");
            }

            DirectoryInfo[] subDirectories = sourceDir.GetDirectories();
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in subDirectories)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                    Console.WriteLine("Copying Sub directory");
                }
            }
        }
    }
}
