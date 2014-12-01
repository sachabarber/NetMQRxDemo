using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NetMQServer
{
    public interface IMainWindowViewModel
    {
        void Start();
        ICommand StartCommand { get; }
        ICommand StopCommand { get; }
    }
}
