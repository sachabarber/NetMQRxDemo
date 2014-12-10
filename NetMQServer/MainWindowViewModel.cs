using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using Common.ViewModels;
using log4net;
using NetMQServer.Ticker;

namespace NetMQServer
{
    public class MainWindowViewModel
    {
        private readonly ITickerPublisher tickerPublisher;
        private readonly ITickerRepository tickerRepository;
        private Random rand;
        private static readonly ILog Log = LogManager.GetLogger(typeof(MainWindowViewModel));
        private CancellationTokenSource autoRunningCancellationToken;
        private Task autoRunningTask;
        private bool serverStarted;
        private bool autoTickerStarted;

        public MainWindowViewModel(ITickerPublisher tickerPublisher, ITickerRepository tickerRepository)
        {
            this.tickerPublisher = tickerPublisher;
            this.tickerRepository = tickerRepository;
            this.rand = new Random();

            serverStarted = false;
            autoTickerStarted = false;

            AutoTickerStartCommand = new DelegateCommand(
                AutoRunning, 
                () => serverStarted && !autoTickerStarted);
            
            AutoTickerStopCommand = new DelegateCommand(
                () =>
                {
                    if (autoRunningCancellationToken != null)
                    {
                        autoRunningCancellationToken.Cancel();
                        autoRunningTask.Wait();
                        autoTickerStarted = false;
                    }
                }, 
                () => serverStarted && autoTickerStarted);

            SendOneTickerCommand = new DelegateCommand(
                SendOneManualFakeTicker, 
                () => serverStarted && !autoTickerStarted);

            StartCommand = new DelegateCommand(
                StartServer, 
                () => !serverStarted);
            
            StopCommand = new DelegateCommand(
                StopServer, 
                () => serverStarted);
        }

        public DelegateCommand AutoTickerStartCommand { get; set; }
        public DelegateCommand AutoTickerStopCommand { get; set; }
        public DelegateCommand SendOneTickerCommand { get; set; }
        public DelegateCommand StartCommand { get; set; }
        public DelegateCommand StopCommand { get; set; }

        public void Start()
        {
            StartServer();
        }


        private void AutoRunning()
        {
            autoTickerStarted = true;
            autoRunningCancellationToken = new CancellationTokenSource();
            autoRunningTask = Task.Run(async () =>
            {
                //Publisher is not thread safe, so while the auto ticker is 
                //running only the autoticker is allowed to access the publisher
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
            serverStarted = true;
            tickerPublisher.Start();
            AutoRunning();
        }

        private void StopServer()
        {
            if (autoTickerStarted)
            {
                autoRunningCancellationToken.Cancel();

                // Publisher is not thread safe, so while the auto ticker is 
                // running only the autoticker is allowed to access the publisher. 
                //Therefore before we can stop the publisher we have to 
                // wait for the autoticker task to complete
                autoRunningTask.Wait();
                autoTickerStarted = false;

                autoRunningCancellationToken = null;
                autoRunningTask = null;
            }
            tickerPublisher.Stop();

            serverStarted = false;
        }
    }
}
