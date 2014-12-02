using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Common.ViewModels;
using log4net;
using NetMQServer.Ticker;

namespace NetMQServer
{
    public class MainWindowViewModel : IMainWindowViewModel
    {
        private readonly ITickerPublisher tickerPublisher;
        private readonly ITickerRepository tickerRepository;
        private Random rand;
        private static readonly ILog Log = LogManager.GetLogger(typeof(MainWindowViewModel));
        private CancellationTokenSource autoRunningCancellationToken;
        private Task autoRunningTask;


        public MainWindowViewModel(ITickerPublisher tickerPublisher, ITickerRepository tickerRepository)
        {
            this.tickerPublisher = tickerPublisher;
            this.tickerRepository = tickerRepository;
            this.rand = new Random();

            AutoTickerStartCommand = new DelegateCommand(AutoRunning);
            AutoTickerStopCommand = new DelegateCommand(() =>
            {
                if (autoRunningCancellationToken != null)
                {
                    autoRunningCancellationToken.Cancel();
                }
            });
            SendOneTickerCommand = new DelegateCommand(SendOneManualFakeTicker);
            StartCommand = new DelegateCommand(StartServer);
            StopCommand = new DelegateCommand(StopServer);
        }

        public ICommand AutoTickerStartCommand { get; set; }
        public ICommand AutoTickerStopCommand { get; set; }
        public ICommand SendOneTickerCommand { get; set; }
        public ICommand StartCommand { get; private set; }
        public ICommand StopCommand { get; private set; }

        public void Start()
        {
            StartServer();
        }

        private void AutoRunning()
        {
            autoRunningCancellationToken = new CancellationTokenSource();
            autoRunningTask = Task.Run(async () =>
            {
                // Publisher is not thread safe, so while the auto ticker is running only the autoticker is allowed to access the publisher

                while (!autoRunningCancellationToken.IsCancellationRequested)
                {
                    SendOneManualFakeTicker();

                    await Task.Delay(20);
                }
            });
        }

        private void SendOneManualFakeTicker()
        {
            var currentTicker = tickerRepository.GetNextTicker();

            var flipPoint = rand.Next(0, 100);

            if (flipPoint > 50)
            {
                currentTicker.Price += currentTicker.Price / 30;
            }
            else
            {
                currentTicker.Price -= currentTicker.Price / 30;
            }

            tickerRepository.StoreTicker(currentTicker);

            tickerPublisher.PublishTrade(currentTicker);
        }

        private void StartServer()
        {
            tickerPublisher.Start();
            AutoRunning();
        }

        private void StopServer()
        {
            if (autoRunningCancellationToken != null)
            {
                autoRunningCancellationToken.Cancel();

                // Publisher is not thread safe, so while the auto ticker is running only the autoticker is allowed to access the publisher
                // Therefore before we can stop the publisher we have to wait for the autoticker task to complete
                autoRunningTask.Wait();

                autoRunningCancellationToken = null;
            }
            tickerPublisher.Stop();
        }
    }
}
