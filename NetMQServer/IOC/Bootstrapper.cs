using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using NetMQ;
using NetMQServer.Ticker;

namespace NetMQServer.IOC
{
    public class Bootstrapper
    {
        public IContainer Build()
        {
            var builder = new ContainerBuilder();
          
            
            // NetMQ
            builder.RegisterInstance(NetMQContext.Create()).SingleInstance();
            builder.RegisterType<NetMQPublisher>().As<ITickerPublisher>().SingleInstance();


            builder.RegisterType<TickerRepository>().As<ITickerRepository>().SingleInstance();

            // UI
            builder.RegisterType<MainWindow>().SingleInstance();
            builder.RegisterType<MainWindowViewModel>().SingleInstance();

            return builder.Build();
        }
    }
}
