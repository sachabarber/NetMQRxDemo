using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Comms.Transport;
using Common;
using NetMQ;

namespace Client.Comms
{
    public class TickerClient : ITickerClient
    {
        private readonly NetMQContext context;
        private readonly string address;

        public TickerClient(NetMQContext context, string address)
        {
            this.context = context;
            this.address = address;
        }

        public IObservable<TickerDto> GetTickerStream()
        {
            return Observable.Create<TickerDto>(observer =>
            {
                NetMQTickerClient client = new NetMQTickerClient(context, address);


               
                var disposable = client.GetTickerStream().Subscribe(observer);
                return new CompositeDisposable { client, disposable };
            })
            .Publish()
            .RefCount();
        }


        public IObservable<ConnectionInfo> ConnectionStatusStream()
        {
            return Observable.Create<ConnectionInfo>(observer =>
            {
                NetMQHeartBeatClient.Instance.InitialiseComms();

                var disposable = NetMQHeartBeatClient.Instance.GetConnectionStatusStream().Subscribe(observer);
                return new CompositeDisposable { disposable };
            })
            .Publish()
            .RefCount();
        }
    }
}
