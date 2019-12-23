using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileBackupService.Helpers
{
    public class DirectoryCompare : IEqualityComparer<DirectoryInfo>
    {
        public bool Equals(DirectoryInfo x, DirectoryInfo y)
        {
            //Only check name for now.
            return x.Name == y.Name;
        }

        public int GetHashCode(DirectoryInfo obj)
        {
            string s = $"{obj.Name}{obj.GetFiles().Count()}";
            return s.GetHashCode();
        }
    }
}
