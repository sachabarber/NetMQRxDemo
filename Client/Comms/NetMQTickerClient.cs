using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Client.Factory;
using Client.Comms.Transport;
using Common;
using NetMQ;
using NetMQ.Actors;
using NetMQ.InProcActors;
using NetMQ.Sockets;
using NetMQ.zmq;
using Newtonsoft.Json;
using Poller = NetMQ.Poller;

namespace Client.Comms
{
    public class NetMQTickerClient : IDisposable
    {
        private Actor<object> actor;
        private Subject<TickerDto> subject;
        private CompositeDisposable disposables = new CompositeDisposable();

        class ShimHandler : IShimHandler<object>
        {
            private NetMQContext context;
            private SubscriberSocket subscriberSocket;
            private Subject<TickerDto> subject;
            private string address;
            private Poller poller;
            private NetMQTimer timeoutTimer;

            public ShimHandler(NetMQContext context, Subject<TickerDto> subject, string address)
            {
                this.context = context;
                this.address = address;
                this.subject = subject;
            }

            public void Initialise(object state)
            {

            }

            public void RunPipeline(PairSocket shim)
            {
                // we should signal before running the poller but this will block the application
                shim.SignalOK();

                this.poller = new Poller();

                shim.ReceiveReady += OnShimReady;
                poller.AddSocket(shim);

                timeoutTimer = new NetMQTimer(StreamingProtocol.Timeout);
                timeoutTimer.Elapsed += TimeoutElapsed;
                poller.AddTimer(timeoutTimer);

                Connect();

                poller.Start();

                if (subscriberSocket != null)
                {
                    subscriberSocket.Dispose();
                }
            }

            private void Connect()
            {
                // getting the snapshot
                using (RequestSocket requestSocket = context.CreateRequestSocket())
                {

                    requestSocket.Connect(string.Format("tcp://{0}:{1}", address, SnapshotProtocol.Port));

                    requestSocket.Send(SnapshotProtocol.GetTradessCommand);

                    string json;

                    requestSocket.Options.ReceiveTimeout = SnapshotProtocol.RequestTimeout;

                    try
                    {
                        json = requestSocket.ReceiveString();
                    }
                    catch (AgainException ex)
                    {
                        // Fail to receive trades, we call on error and don't try to do anything with subscriber
                        // calling on error from poller thread block the application
                        Task.Run(() => subject.OnError(new Exception("No response from server")));
                        return;
                    }

                    while (json != SnapshotProtocol.EndOfTickers)
                    {
                        PublishTicker(json);

                        json = requestSocket.ReceiveString();
                    }
                }

                subscriberSocket = context.CreateSubscriberSocket();
                subscriberSocket.Subscribe(StreamingProtocol.TradesTopic);
                subscriberSocket.Subscribe(StreamingProtocol.HeartbeatTopic);
                subscriberSocket.Connect(string.Format("tcp://{0}:{1}", address, StreamingProtocol.Port));
                subscriberSocket.ReceiveReady += OnSubscriberReady;

                poller.AddSocket(subscriberSocket);

                // reset timeout timer
                timeoutTimer.Enable = false;
                timeoutTimer.Enable = true;
            }

            private void TimeoutElapsed(object sender, NetMQTimerEventArgs e)
            {
                // no need to reconnect, the client would be recreated because of RX

                // because of RX internal stuff invoking on the poller thread block the entire application, so calling on Thread Pool
                Task.Run(() => subject.OnError(new Exception("Disconnected from server")));
            }

            private void OnShimReady(object sender, NetMQSocketEventArgs e)
            {
                string command = e.Socket.ReceiveString();

                if (command == ActorKnownMessages.END_PIPE)
                {
                    poller.Stop(false);
                }
            }

            private void OnSubscriberReady(object sender, NetMQSocketEventArgs e)
            {
                string topic = subscriberSocket.ReceiveString();

                if (topic == StreamingProtocol.TradesTopic)
                {
                    string json = subscriberSocket.ReceiveString();
                    PublishTicker(json);

                    // reset timeout timer also when a quote is received
                    timeoutTimer.Enable = false;
                    timeoutTimer.Enable = true;
                }
                else if (topic == StreamingProtocol.HeartbeatTopic)
                {
                    // reset timeout timer
                    timeoutTimer.Enable = false;
                    timeoutTimer.Enable = true;
                }
            }

            private void PublishTicker(string json)
            {
                TickerDto tickerDto = JsonConvert.DeserializeObject<TickerDto>(json);
                subject.OnNext(tickerDto);
            }
        }

        public NetMQTickerClient(NetMQContext context, string address)
        {
            subject = new Subject<TickerDto>();

            this.actor = new Actor<object>(context, new ShimHandler(context, subject, address), null);
            this.disposables.Add(this.actor);

            this.disposables.Add(NetMQHeartBeatClient.Instance.GetConnectionStatusStream()
                .Where(x => x.ConnectionStatus == ConnectionStatus.Closed)
                .Subscribe(x =>
                    this.subject.OnError(new InvalidOperationException("Connection to server has been lost"))));
        }

        public IObservable<TickerDto> GetTickerStream()
        {
            return subject.AsObservable();
        }

        public void Dispose()
        {
            this.disposables.Dispose();
        }
    }
}
