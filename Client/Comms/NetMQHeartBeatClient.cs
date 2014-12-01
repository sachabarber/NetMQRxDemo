using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
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
    public class NetMQHeartBeatClient 
    {
        private readonly NetMQContext context;
        private readonly string address;
        private Actor<object> actor;
        private Subject<ConnectionInfo> subject;
        private static NetMQHeartBeatClient instance = null;
        private static object syncLock = new object();
        protected int requiresInitialisation = 1;

        class ShimHandler : IShimHandler<object>
        {
            private NetMQContext context;
            private SubscriberSocket subscriberSocket;
            private Subject<ConnectionInfo> subject;
            private string address;
            private Poller poller;
            private NetMQTimer timeoutTimer;
            private NetMQHeartBeatClient parent;

            public ShimHandler(NetMQContext context, Subject<ConnectionInfo> subject, string address)
            {
                this.context = context;
                this.address = address;
                this.subject = subject;
            }

            public void Initialise(object state)
            {
                parent = (NetMQHeartBeatClient) state;
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
                subscriberSocket = context.CreateSubscriberSocket();
                subscriberSocket.Subscribe(StreamingProtocol.HeartbeatTopic);
                subscriberSocket.Connect(string.Format("tcp://{0}:{1}", address, StreamingProtocol.Port));

                subject.OnNext(new ConnectionInfo(ConnectionStatus.Connecting, this.address));


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
                Task.Run(() =>
                {
                    parent.requiresInitialisation = 1;
                    subject.OnNext(new ConnectionInfo(ConnectionStatus.Closed, this.address));
                });
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

                if (topic == StreamingProtocol.HeartbeatTopic)
                {
                    subject.OnNext(new ConnectionInfo(ConnectionStatus.Connected, this.address));

                    // reset timeout timer
                    timeoutTimer.Enable = false;
                    timeoutTimer.Enable = true;
                }
            }

        }





 

        private NetMQHeartBeatClient(NetMQContext context, string address)
        {
            this.context = context;
            this.address = address;
            InitialiseComms();
        }

        public static NetMQHeartBeatClient CreateInstance(NetMQContext context, string address)
        {
            if (instance == null)
            {
                lock (syncLock)
                {
                    if (instance == null)
                    {
                        instance = new NetMQHeartBeatClient(context,address);
                    }
                }
            }
            return instance;
        }

        public void InitialiseComms()
        {

            if (Interlocked.CompareExchange(ref requiresInitialisation, 0, 1) == 1)
            {
                if (actor != null)
                {
                    this.actor.Dispose();
                }

                subject = new Subject<ConnectionInfo>();
                this.actor = new Actor<object>(context, new ShimHandler(context, subject, address), this);
            }


        }

        public IObservable<ConnectionInfo> GetConnectionStatusStream()
        {
            return subject.AsObservable();
        }


        public static NetMQHeartBeatClient Instance
        {
            get { return instance; }

        }


        
        
 
    }
}
