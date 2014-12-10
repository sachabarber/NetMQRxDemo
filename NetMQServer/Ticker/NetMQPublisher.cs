using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using Common;
using NetMQ;
using NetMQ.Actors;
using NetMQ.InProcActors;
using NetMQ.Sockets;
using NetMQ.zmq;
using Newtonsoft.Json;
using Poller = NetMQ.Poller;

namespace NetMQServer.Ticker
{
    public class NetMQPublisher : ITickerPublisher
    {
        private const string PublishTicker = "P";        

        public class ShimHandler : IShimHandler<object>
        {
            private readonly NetMQContext context;
            private PublisherSocket publisherSocket;
            private ResponseSocket snapshotSocket;
            private ITickerRepository tickerRepository;
            private Poller poller;
            private NetMQTimer heartbeatTimer;

            public ShimHandler(NetMQContext context, ITickerRepository tickerRepository)
            {
                this.context = context;
                this.tickerRepository = tickerRepository;                
            }

            public void Initialise(object state)
            {

            }

            public void RunPipeline(PairSocket shim)
            {
                publisherSocket = context.CreatePublisherSocket();
                publisherSocket.Bind("tcp://*:" + StreamingProtocol.Port);

                snapshotSocket = context.CreateResponseSocket();
                snapshotSocket.Bind("tcp://*:" + SnapshotProtocol.Port);
                snapshotSocket.ReceiveReady += OnSnapshotReady;
                
                shim.ReceiveReady += OnShimReady;

                heartbeatTimer = new NetMQTimer(StreamingProtocol.HeartbeatInterval);
                heartbeatTimer.Elapsed += OnHeartbeatTimerElapsed;

                shim.SignalOK();

                poller = new Poller();
                poller.AddSocket(shim);
                poller.AddSocket(snapshotSocket);
                poller.AddTimer(heartbeatTimer);
                poller.Start();

                publisherSocket.Dispose();
                snapshotSocket.Dispose();
            }

            private void OnHeartbeatTimerElapsed(object sender, NetMQTimerEventArgs e)
            {
                publisherSocket.Send(StreamingProtocol.HeartbeatTopic);
            }

            private void OnSnapshotReady(object sender, NetMQSocketEventArgs e)
            {                
                string command = snapshotSocket.ReceiveString();

                // Currently we only have one type of events
                if (command == SnapshotProtocol.GetTradessCommand)
                {
                    var tickers = tickerRepository.GetAllTickers();

                    // we will send all the tickers in one message
                    foreach (var ticker in tickers)
                    {
                        snapshotSocket.SendMore(JsonConvert.SerializeObject(ticker));
                    }

                    snapshotSocket.Send(SnapshotProtocol.EndOfTickers);
                }
            }

            private void OnShimReady(object sender, NetMQSocketEventArgs e)
            {
   
                string command = e.Socket.ReceiveString();

                switch (command)
                {
                    case ActorKnownMessages.END_PIPE:
                        poller.Stop(false);
                        break;
                    case PublishTicker:
                        string topic = e.Socket.ReceiveString();
                        string json = e.Socket.ReceiveString();
                        publisherSocket.
                            SendMore(topic).
                            Send(json);
                        break;
                }

            }
        }

        private Actor<object> actor;
        private readonly NetMQContext context;
        private readonly ITickerRepository tickerRepository;
                    
        public NetMQPublisher(NetMQContext context, ITickerRepository tickerRepository)
        {
            this.context = context;
            this.tickerRepository = tickerRepository;        
        }

        public void Start()
        {
            if (actor != null)
                return;

            actor = new Actor<object>(context, new ShimHandler(context, tickerRepository), null);
        }

        public void Stop()
        {
            if (actor != null)
            {
                actor.Dispose();
                actor = null;
            }
        }        

        public void PublishTrade(TickerDto ticker)
        {
            if (actor == null)
                return;

            actor.
                SendMore(PublishTicker).
                SendMore(StreamingProtocol.TradesTopic).
                Send(JsonConvert.SerializeObject(ticker));                
        }

    }
}
