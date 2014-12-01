using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Autofac;
using NetMQServer.Ticker;
using NetMQServer.IOC;
using log4net;

namespace NetMQServer
{ 
    public partial class App : Application
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(App));
        public static IContainer Container;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            InitializeLogging();
            Start();
        }

        private void Start()
        {
            var bootstrapper = new Bootstrapper();
            var container = bootstrapper.Build();

            // expose via static variable so SignalR can pick it up in Startup class 
            Container = container;

            var mainWindow = container.Resolve<MainWindow>();
            var vm = container.Resolve<MainWindowViewModel>();
            mainWindow.DataContext = vm;
            vm.Start();
            mainWindow.Show();
        }

        private void InitializeLogging()
        {
            Thread.CurrentThread.Name = "UI";

            log4net.Config.XmlConfigurator.Configure();

            Log.Info(@"NetMQServer started");
        }
    }
}
