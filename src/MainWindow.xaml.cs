using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WpfGraph
{    
    public partial class MainWindow : Window
    {
        DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();
            _timer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 20) };
            _timer.Tick += OnTick;

            RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
        }

        void OnTick(object sender, EventArgs e)
        {
            AddVirtualData();
            m_liveGraph.InvalidateVisual();
            
        }

        void AddVirtualData()
        {
            Random rnd = new Random();

            float volatge = rnd.Next(10, 13);
            float current = rnd.Next(0, 7);

            m_liveGraph.AddDataset(new float[] { current, volatge });
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            m_liveGraph.AddChannel("Current", Brushes.Green);
            m_liveGraph.AddChannel("Voltage", Brushes.Red);

            _timer.Start();
        }
    }
}
