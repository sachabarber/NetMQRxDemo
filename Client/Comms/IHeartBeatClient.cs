using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Comms.Transport;
using Common;

namespace Client.Hub
{
    public interface IHeartBeatClient
    {
        IObservable<ConnectionInfo> ConnectionStatusStream();
    }
}
