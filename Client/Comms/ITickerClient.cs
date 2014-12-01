using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace Client.Comms
{
    internal interface ITickerClient
    {
        IObservable<TickerDto> GetTickerStream();
    }
}
