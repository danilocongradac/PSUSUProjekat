using PLCSimulator;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;

namespace ScadaGUI
{
    public partial class PLC : Window
    {
        private PLCSimulatorManager plc;
        private DispatcherTimer timer;

        public PLC()
        {
            InitializeComponent();

            plc = new PLCSimulatorManager();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += Timer_Tick;
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            plc.StartPLCSimulator();
            timer.Start();
            txtStatus.Text = "Status: Running";
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            plc.Abort();
            timer.Stop();
            txtStatus.Text = "Status: Stopped";
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var values = new List<KeyValuePair<string, double>>();

            foreach (var addr in new[]
            {
                "ADDR001","ADDR002","ADDR003","ADDR004",
                "ADDR005","ADDR006","ADDR007","ADDR008",
                "ADDR009","ADDR010","ADDR011","ADDR012",
                "ADDR013","ADDR014","ADDR015","ADDR016"
            })
            {
                values.Add(new KeyValuePair<string, double>(addr, plc.GetValue(addr)));
            }

            dgValues.ItemsSource = values;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            try { plc.Abort(); } catch { }
        }
    }
}
