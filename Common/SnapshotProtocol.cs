using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public static class SnapshotProtocol
    {
        public const string GetTradessCommand = "GT";
        public const string EndOfTickers = "~~~EOT";
        public const int Port = 5264;
        public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);
    }
}
