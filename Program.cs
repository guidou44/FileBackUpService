using FileBackupService.Helpers;
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
        private static bool letProcessEvent = false;

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
                watcher.NotifyFilter = 
                                      NotifyFilters.FileName
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

        private static void DeleteUnmatchingFilesFolders(string sourcePath, string destPath)
        {
            var source = new DirectoryInfo(sourcePath);
            var dest = new DirectoryInfo(destPath);

            //Files
            var sourceFiles = source.GetFiles();
            var destFiles = dest.GetFiles();
            var fileCompare = new FileCompare();

            var destOnlyFiles = (from file in destFiles select file).Except(sourceFiles, fileCompare);

            foreach (var file in destOnlyFiles)
            {
                File.Delete(file.FullName);
                Console.WriteLine($"Deleted file {file.FullName}");
            }

            //Directories
            var sourceSubDir = source.GetDirectories();
            var destSubDir = dest.GetDirectories();
            var dirCompare = new DirectoryCompare();

            var destOnlyDir = (from sub in destSubDir select sub).Except(sourceSubDir, dirCompare);

            foreach (var sub in destOnlyDir)
            {
                Directory.Delete(sub.FullName, true);
                Console.WriteLine($"Deleted directory {sub.FullName}");
            }
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
            //if (!Directory.Exists(destDirName)) Directory.CreateDirectory(destDirName);

            if (!IsDirecotry)
            {
                var file = source as FileInfo;
                var subPath = GetSubPath(file);
                var tempPath = Path.Combine(destDirName, subPath);
                file.CopyTo(tempPath, true);
                Console.WriteLine($"Copying file {file.FullName} to {tempPath}");
                var tempDir = new DirectoryInfo(tempPath);
                DeleteUnmatchingFilesFolders(file.Directory.FullName, tempDir.Parent.FullName);
                return;
            }

            var directory = source as DirectoryInfo;
            var subDirPath = GetSubPath(directory);
            var DirectoryDestinationPath = Path.Combine(destDirName, subDirPath);
            if (!Directory.Exists(DirectoryDestinationPath))
            {
                Directory.CreateDirectory(DirectoryDestinationPath);
                Console.WriteLine($"Creating {directory.FullName} to {DirectoryDestinationPath}");
                var tempDirectory = new DirectoryInfo(DirectoryDestinationPath);
                DeleteUnmatchingFilesFolders(directory.Parent.FullName, tempDirectory.Parent.FullName);
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

        private static string GetSubPath(FileSystemInfo fileSysInfo)
        {
            var outputPath = fileSysInfo.Name;
            var directParent = fileSysInfo;


            while (GetDirectParent(directParent).FullName != _sourceDirName)
            {
                outputPath = Path.Combine(GetDirectParent(directParent).Name, outputPath);
                directParent = GetDirectParent(directParent);
            }

            return outputPath;
        }

        private static DirectoryInfo GetDirectParent(FileSystemInfo child)
        {
            FileInfo fileChild = null;
            DirectoryInfo directoryChild = null;
            if (child.GetType().Name == nameof(DirectoryInfo))
            {
                directoryChild = child as DirectoryInfo;
                return directoryChild.Parent;
            }
            else if (child.GetType().Name == nameof(FileInfo))
            {
                fileChild = child as FileInfo;
                return fileChild.Directory;
            }

            else throw new ArgumentException("Unmanaged type of FileSysteminfo");
        }
    }
}
