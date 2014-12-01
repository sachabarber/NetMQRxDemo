using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Factory;
using Client.Comms;
using Client.Comms.Transport;
using Client.Hub;
using Client.Repositories;
using log4net;
using NetMQ;

namespace Client.Services
{
    public class ReactiveTrader : IReactiveTrader, IDisposable
    {
        
        private static readonly ILog log = LogManager.GetLogger(typeof(ReactiveTrader));

        private NetMQContext context;


        public void Initialize(string username, string server)
        {        
            var concurrencyService = new ConcurrencyService();
            
            context = NetMQContext.Create();

            var tickerClient = new TickerClient(context, server);
            var netMQHeartBeatClient = NetMQHeartBeatClient.CreateInstance(context, server);
            HeartBeatClient = new HeartBeatClient();

            var tickerFactory = new TickerFactory();
            TickerRepository = new TickerRepository(tickerClient, tickerFactory);
        }

        public ITickerRepository TickerRepository { get; private set; }
        public IHeartBeatClient HeartBeatClient { get; private set; }


        public IObservable<ConnectionInfo> ConnectionStatusStream
        {
            get
            {
                return HeartBeatClient.ConnectionStatusStream()
                    .Repeat()
                    .Publish()
                    .RefCount();
            }
        }

        public void Dispose()
        {
            context.Dispose();
        }
    }

}
