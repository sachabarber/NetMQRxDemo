using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Client.Factory;
using Client.Comms;

namespace Client.Repositories
{
    class TickerRepository : ITickerRepository
    {
        private readonly ITickerClient tickerClient;
        private readonly ITickerFactory tickerFactory;

        public TickerRepository(ITickerClient tickerClient, ITickerFactory tickerFactory)
        {
            this.tickerClient = tickerClient;
            this.tickerFactory = tickerFactory;
        }

        public IObservable<Ticker> GetTickerStream()
        {
            return Observable.Defer(() => tickerClient.GetTickerStream())
                .Select(tickerFactory.Create)                
                .Catch<Ticker>(Observable.Empty<Ticker>())
                .Repeat()
                .Publish()
                .RefCount();
        }

    }
}
