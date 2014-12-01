using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Comms;
using Client.Comms.Transport;
using NetMQ;

namespace Client.Hub
{
    public class HeartBeatClient : IHeartBeatClient
    {
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
