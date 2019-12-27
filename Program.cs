

using System.Threading;

namespace FileBackupService
{
    class Program
    {
        static Thread worker;
        static void Main(string[] args)
        {
            worker = new Thread(DoWork);
            worker.Start();
        }

        static void DoWork(object args)
        {
            FileWatchManager _manager = new FileWatchManager();
            _manager.Start();
        }
    }
}
