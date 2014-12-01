using System.Threading;
using System.Windows;
using Autofac;
using Client.Comms;
using Client.IOC;
using Client.Services;
using log4net;


namespace Client
{
 
    public partial class App : Application
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(App));

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

            Log.Info("Initializing reactive trader API...");
            var reactiveTraderApi = container.Resolve<IReactiveTrader>();

            var username = container.Resolve<IUserProvider>().Username;
            reactiveTraderApi.Initialize(username, "localhost");

            var mainWindow = container.Resolve<MainWindow>();
            //var vm = container.Resolve<MainWindowViewModel>();
            //mainWindow.DataContext = vm;
            mainWindow.Show();




            var netMQHeartClient = NetMQHeartBeatClient.CreateInstance(null, "ghhasa");


        }

        private void InitializeLogging()
        {
            Thread.CurrentThread.Name = "UI";
            log4net.Config.XmlConfigurator.Configure();
            Log.Info(@"SignalRSelfHost started");
        }            
    }
}
