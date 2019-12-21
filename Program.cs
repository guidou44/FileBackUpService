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



        private static void WhatchForFileChange()
        {
            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                watcher.Path = _sourceDirName;
                watcher.NotifyFilter = NotifyFilters.LastWrite
                                     | NotifyFilters.FileName
                                     | NotifyFilters.DirectoryName;
                watcher.IncludeSubdirectories = true;
                watcher.Changed += OnChanged;
                watcher.Created += OnChanged;
                watcher.Renamed += OnChanged;
                watcher.EnableRaisingEvents = true;

                while (true)
                {
                    Thread.Sleep(5000);
                }
            }
        }

        static void InitializeComponent()
        {
            _sourceDirName = @"" + ConfigurationManager.AppSettings.Get("SourceFile");
            _destinationDirName = @"" + ConfigurationManager.AppSettings.Get("DestinationFile");
        }

        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            Thread.Sleep(1000);
            DirectoryCopy(e.FullPath, _destinationDirName, true);
        }

        private static void DirectoryCopy(string sourceName, string destDirName, bool copySubDirs = true)
        {
            bool IsDirecotry = false;
            FileSystemInfo source;
            FileAttributes sourceAttribute = File.GetAttributes(sourceName);
            if (sourceAttribute.HasFlag(FileAttributes.Directory))
            {
                source = new DirectoryInfo(sourceName);
                IsDirecotry = true;
            }

            else source = new FileInfo(sourceName);

            if (!source.Exists) throw new ArgumentNullException("Specified source Directory/File does not exist");
            if (!Directory.Exists(destDirName)) Directory.CreateDirectory(destDirName);

            if (!IsDirecotry)
            {
                var file = source as FileInfo;
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, true);
                Console.WriteLine($"Copying file {file.Name}");
                return;
            }

            var directory = source as DirectoryInfo;
            var subDirPath = GetSubDirectoryLocalPath(directory);
            var DirectoryDestinationPath = Path.Combine(destDirName, subDirPath);
            if (!Directory.Exists(DirectoryDestinationPath))
            {
                Directory.CreateDirectory(DirectoryDestinationPath);
                Console.WriteLine($"Creating {directory.Name}");
            }

            FileInfo[] files = directory.GetFiles();

            foreach (FileInfo file in files)
            {
                try
                {
                    string temppath = Path.Combine(DirectoryDestinationPath, file.Name);
                    file.CopyTo(temppath, true);
                    Console.WriteLine("Copying file");
                }
                catch (Exception e)
                {
                    //Common.Reports.Reporter.LogException(e);
                    continue;
                }
            }

            DirectoryInfo[] subDirectories = directory.GetDirectories();
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in subDirectories)
                {
                    if (subdir.Name.Contains(".git")) continue;
                    string tempPath = Path.Combine(DirectoryDestinationPath, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                    Console.WriteLine($"Copying Sub directory {subdir.Name}");
                }
            }
        }

        private static string GetSubDirectoryLocalPath(DirectoryInfo subDir)
        {
            var outputPath = subDir.Name;
            var directParent = subDir;
            while (directParent.Parent.FullName != _sourceDirName)
            {
                outputPath = Path.Combine(directParent.Parent.Name, outputPath);
                directParent = directParent.Parent;
            }

            return outputPath;
        }
    }
}
