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
        private HookObserverViewModel _vm;

        public HookObserver()
        {
            InitializeComponent();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _vm?.Dispose();
            _vm = null;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_vm != null) _vm.Dispose();
            _vm = new HookObserverViewModel();
            this.DataContext = _vm;
        }
    }
}
