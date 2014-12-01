using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public static class StreamingProtocol
    {
        public const int Port = 5263;
        public const string TradesTopic = "Trades";
        public const string HeartbeatTopic = "HB";
        public static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(2);
        public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    }
}
