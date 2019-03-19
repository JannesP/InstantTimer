using InstantTimer.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace InstantTimer.View.Controls
{
    /// <summary>
    /// Interaction logic for HookObserver.xaml
    /// </summary>
    public partial class HookObserver : UserControl
    {
        public HookObserver()
        {
            InitializeComponent();
            this.DataContext = new HookProcViewModel();
        }
    }
}
