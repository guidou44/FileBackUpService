

namespace FileBackupService
{
    class Program
    {
        static void Main(string[] args)
        {
            FileWatchManager _manager = new FileWatchManager();
            _manager.Start();
        }
    }
}
