using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Comms.Transport;
using Client.Services;
using log4net;

namespace Client.ViewModels
{
    public class ConnectivityStatusViewModel : INPCBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ConnectivityStatusViewModel));
        private string server;
        private string status;
        private bool disconnected;



        public ConnectivityStatusViewModel(
            IReactiveTrader reactiveTrader,
            IConcurrencyService concurrencyService)
        {

          
            reactiveTrader.ConnectionStatusStream
                .ObserveOn(concurrencyService.Dispatcher)
                .SubscribeOn(concurrencyService.TaskPool)
                .Subscribe(
                OnStatusChange,
                ex => log.Error("An error occurred within the connection status stream.", ex));

        }


        private void OnStatusChange(ConnectionInfo connectionInfo)
        {
            Server = connectionInfo.Server;

            switch (connectionInfo.ConnectionStatus)
            {
                case ConnectionStatus.Connecting:
                    Status = "Connecting...";
                    Disconnected = true;
                    break;
                case ConnectionStatus.Connected:
                    Status = "Connected";
                    Disconnected = false;
                    break;
                case ConnectionStatus.Closed:
                    Status = "Disconnected";
                    Disconnected = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        public string Server
        {
            get { return this.server; }
            set
            {
                this.server = value;
                base.OnPropertyChanged("Server");
            }
        }







        public string Status
        {
            get { return this.status; }
            set
            {
                this.status = value;
                base.OnPropertyChanged("Status");
            }
        }



        public bool Disconnected
        {
            get { return this.disconnected; }
            set
            {
                this.disconnected = value;
                base.OnPropertyChanged("Disconnected");
            }
        }
      
    }

}
