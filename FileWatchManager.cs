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
    public class FileWatchManager
    {
        private string _sourceDirName;
        private string _destinationDirName;
        private FileSystemWatcher _watcher;
        private bool _isRunning;

        public FileWatchManager()
        {
            InitializeComponent();
        }

        public void Start()
        {
            _isRunning = true;
            WhatchForFileChange();
        }

        public void Stop()
        {
            _isRunning = false;
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnChanged;
            _watcher.Created -= OnChanged;
            _watcher.Renamed -= OnChanged;
            _watcher.Dispose();
        }

        #region Private Methods

        private void DeleteUnmatchingFilesFolders(string sourcePath, string destPath)
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
                try
                {
                    File.Delete(file.FullName);
                }
                catch (Exception e)
                {
                    Common.Reports.Reporter.LogException(e);
                }
            }

            //Directories
            var sourceSubDir = source.GetDirectories();
            var destSubDir = dest.GetDirectories();
            var dirCompare = new DirectoryCompare();

            var destOnlyDir = (from sub in destSubDir select sub).Except(sourceSubDir, dirCompare);

            foreach (var sub in destOnlyDir)
            {
                try
                {
                    Directory.Delete(sub.FullName, true);
                }
                catch (Exception e)
                {
                    Common.Reports.Reporter.LogException(e);
                }
            }
        }

        private void DirectoryCopy(string sourceName, string destDirName, bool copySubDirs = true)
        {
            bool IsDirecotry = false;
            FileSystemInfo source;
            FileAttributes sourceAttribute = FileAttributes.Offline;
            try
            {
                sourceAttribute = File.GetAttributes(sourceName);

            }
            catch (Exception e)
            {
                return;
            }
            
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
                    var tempSubDir = new FileInfo(tempPath).Directory.FullName;
                try
                {
                    InstantiateMissingSubDirectories(tempSubDir);
                    file.CopyTo(tempPath, true);
                    var tempDir = new DirectoryInfo(tempPath);
                    DeleteUnmatchingFilesFolders(file.Directory.FullName, tempDir.Parent.FullName);
                }
                catch (Exception e)
                {
                    Common.Reports.Reporter.LogException(e);
                    return;
                }
                return;
            }

            var directory = source as DirectoryInfo;
            var subDirPath = GetSubPath(directory);
            var DirectoryDestinationPath = Path.Combine(destDirName, subDirPath);
            if (!Directory.Exists(DirectoryDestinationPath))
            {
                Directory.CreateDirectory(DirectoryDestinationPath);
                var tempDirectory = new DirectoryInfo(DirectoryDestinationPath);
                try
                {
                    DeleteUnmatchingFilesFolders(directory.Parent.FullName, tempDirectory.Parent.FullName);
                }
                catch (Exception e)
                {
                    Common.Reports.Reporter.LogException(e);
                }
            }

            FileInfo[] files = directory.GetFiles();

            foreach (FileInfo file in files)
            {
                try
                {
                    string temppath = Path.Combine(DirectoryDestinationPath, file.Name);
                    file.CopyTo(temppath, true);
                }
                catch (Exception e)
                {
                    Common.Reports.Reporter.LogException(e);
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
                }
            }
        }

        private string GetSubPath(FileSystemInfo fileSysInfo)
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

        private DirectoryInfo GetDirectParent(FileSystemInfo child)
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

        private void InitializeComponent()
        {
            _sourceDirName = @"" + ConfigurationManager.AppSettings.Get("SourceFile");
            _destinationDirName = @"" + ConfigurationManager.AppSettings.Get("DestinationFile");
        }

        private void InstantiateMissingSubDirectories(string path)
        {
            var subDirToInstantiate = new DirectoryInfo(path);
            if (!subDirToInstantiate.Exists) subDirToInstantiate.Create();
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            DirectoryCopy(e.FullPath, _destinationDirName, true);
        }

        private void WhatchForFileChange()
        {
            using (_watcher = new FileSystemWatcher())
            {
                _watcher.Path = _sourceDirName;
                _watcher.NotifyFilter =
                                      NotifyFilters.FileName
                                     | NotifyFilters.DirectoryName;
                _watcher.IncludeSubdirectories = true;
                _watcher.Changed += OnChanged;
                _watcher.Created += OnChanged;
                _watcher.Renamed += OnChanged;
                _watcher.EnableRaisingEvents = true;

                while (_isRunning)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        #endregion
    }
}
