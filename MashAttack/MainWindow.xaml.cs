using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Windows.Forms;
//using System.Timers;
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
using System.Windows.Forms.DataVisualization.Charting;
using OxyPlot;
using OxyPlot.Series;
using System.IO.Ports;
using System.IO;
using OxyPlot.Axes;

namespace MashAttack
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DispatcherTimer myTimer;
        DispatcherTimer updateTimer;
        bool mashEnabled;
        MashSet mashes;
        MashSet mashes2;
        bool first;
        System.Diagnostics.Stopwatch mystop;
        long prior;
        long current;
        long release;
        bool isDown;
        bool isUp;
        int countdown;
        public PlotModel myModel;
        const bool kb_mode = false;
        const int BAUD_RATE = 115200;
        bool timeout = false;
        const int PERIOD = 2;
        long downval = 0;
        long downval2 = 0;
        bool onebutton = true;
        SheetsAgent sheets;
        //string comString;

        delegate void SetStatusCallback(string text);

        SerialComms serial;
        List<byte> _localBuffer;

        const byte SPINUP_SUCCESS = 128;
        const byte SPINUP_FAIL = 64;
        const byte ABORT = 255;
        const byte START = 0xAA;

        //private delegate void UpdateStatsDel();

        public MainWindow()
        {
            InitializeComponent();
            //OxyPlot.Model

            _localBuffer = new List<byte>();
            sheets = new MashAttack.SheetsAgent();
            
            myTimer = new DispatcherTimer(DispatcherPriority.Normal);
            myTimer.Interval = new TimeSpan(0, 0, 0, 10, 0);
            myTimer.Tick += new EventHandler(TimeOver);
            

            updateTimer = new DispatcherTimer(DispatcherPriority.Normal);
            updateTimer.Interval = new TimeSpan(0, 0, 0, 1, 0);
            updateTimer.Tick += new EventHandler(SecondElapsed);
            //myTimer.AutoReset = false;
            //myTimer.SynchronizingObject = (System.ComponentModel.ISynchronizeInvoke)this.avgHz;
            mashEnabled = false;
            mashes = new MashSet();
            mashes2 = new MashSet();
            first = true;
            mystop = new System.Diagnostics.Stopwatch();
            prior = 0;
            updatePortList();
            //myModel = new PlotModel { Title = "Mashing Rate" };

            configBox.Items.Insert(0, "10s 1B");
            configBox.Items.Insert(1, "5s 1B");
            configBox.Items.Insert(2, "30s 1B");

            configBox.Items.Insert(3, "10s 2B");
            configBox.Items.Insert(4, "5s 2B");
            configBox.Items.Insert(5, "30s 2B");

            configBox.SelectedIndex = 0;

        }

        void updatePortList()
        {
            comItems.Items.Clear();
            MenuItem newItem = new MenuItem();
            newItem.Header = "None";
            newItem.IsCheckable = true;
            newItem.IsChecked = true;
            newItem.Click += new RoutedEventHandler(COMSelect);
            comItems.Items.Add(newItem);
            foreach (string s in SerialPort.GetPortNames())
            {
                comItems.Items.Add(new MenuItem());
                newItem = (MenuItem)comItems.Items.GetItemAt(comItems.Items.Count - 1);
                newItem.Header = s;
                newItem.IsCheckable = true;
                newItem.IsChecked = false;
                newItem.Click += new RoutedEventHandler(COMSelect);
                //comItems.Items.Add(newItem);
            }

            if(serial != null)
                if (serial.isOpen())
                    serial.Close();
        }

        private void COMSelect(object sender, EventArgs e)
        {
            MenuItem tempItem;

            for(int i = 0; i < comItems.Items.Count; i++)
            {
                tempItem = (MenuItem)comItems.Items.GetItemAt(i);
                tempItem.IsChecked = false;
            }

            tempItem = (MenuItem)sender;
            tempItem.IsChecked = true;

            if(serial!=null)
                if (serial.isOpen())
                    serial.Close();
            if (tempItem.Header.ToString() != "None")
            {
                serial = new SerialComms(tempItem.Header.ToString(), BAUD_RATE, PERIOD, Status);
                //serial.StatusDelegate = Status;
                serial.BarDelegate = Bar;
                serial.CountdownDelegate = StartCountdown;
                serial.UpdateDelegate = StatsInvoke;
                serial.MashDelegate = MashIncrement;
            }
            //comString = tempItem.Header.ToString();

            //statusLine.Content = comString;
        }

        private void StartComms()
        {
            //_datPort.DiscardOutBuffer();//Flush the output buffer
            //_datPort.DiscardInBuffer();
            if(serial == null)
            {
                Status("Not connected to COM port!");
                return;
            }

            serial.Command(START);//Write out control data to Arduino
            Status("Start signal sent.");

            switch (configBox.SelectedIndex)
            {
                case 0:
                    serial.Command(10);
                    serial.Command(0x41);
                    onebutton = true;
                    countdown = 10;
                    break;
                case 1:
                    serial.Command(5);
                    serial.Command(0x41);
                    onebutton = true;
                    countdown = 5;
                    break;
                case 2:
                    serial.Command(30);
                    serial.Command(0x41);
                    onebutton = true;
                    countdown = 30;
                    break;
                case 3:
                    serial.Command(10);
                    serial.Command(0xC3);
                    onebutton = false;
                    countdown = 10;
                    break;
                case 4:
                    serial.Command(5);
                    serial.Command(0xC3);
                    onebutton = false;
                    countdown = 5;
                    break;
                case 5:
                    serial.Command(30);
                    serial.Command(0xC3);
                    onebutton = false;
                    countdown = 30;
                    break;
                default:
                    serial.Command(10);
                    serial.Command(0x81);
                    onebutton = true;
                    countdown = 10;
                    break;
            }

            Countdown(countdown);
        }

        public void StartCountdown()
        {
            //mystop.Start();
            myTimer.Interval = new TimeSpan(0, 0, 0, countdown, 0);
            myTimer.Start();
            updateTimer.Start();
            Countdown(countdown);
            Bar(Brushes.Green);
        }

        public void Status(string v)
        {
            statusBox.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                   new Action(delegate ()
                                   {
                                       statusBox.AppendText(String.Format(v + "\n"));
                                       statusBox.CaretIndex = statusBox.Text.Length;
                                       statusBox.ScrollToEnd();
                                       statusLine.Content = v;
                                   }));

        }

        public void Bar(Brush mycolor)
        {
            statusBar.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                   new Action(delegate ()
                                   {
                                       statusBar.Fill = mycolor;
                                   }));
        }

        public void Countdown(int num)
        {
            timerLabel.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                   new Action(delegate ()
                                   {
                                       timerLabel.Content = num;
                                   }));
        }

        private void SecondElapsed(object sender, EventArgs e)
        {
            Countdown(--countdown);
            //Status("Timer updated");
        }

        private void PlotResults(long median, long median2)
        {
            PlotModel tmp = new PlotModel { Title = "Mash Rate" };
            
            LineSeries mySeries = new LineSeries { StrokeThickness = 2, Color=OxyColors.PaleVioletRed, MarkerSize = 3, MarkerStroke = OxyColors.ForestGreen, MarkerType = MarkerType.Plus };
            //myModel.Clear();
            LineSeries medLine = new LineSeries { StrokeThickness = 1, Color = OxyColors.DarkRed, LineStyle = LineStyle.Dash, MarkerType = MarkerType.None };
            int i;
            long test = 0;
            long timestamp = 0;

            for (i = 0; i < mashes.count; i++)
            {
                test = mashes.GetMash(i);
                timestamp += test;
                //Console.WriteLine(test);
                mySeries.Points.Add(new OxyPlot.DataPoint(Math.Round(timestamp/1000.0,3), Math.Round(1000.0 / test, 2)));
                
            }
            medLine.Points.Add(new OxyPlot.DataPoint(0.00, Math.Round(1000.0 / median, 2)));
            medLine.Points.Add(new OxyPlot.DataPoint(Math.Ceiling(timestamp / 1000.00), Math.Round(1000.0 / median, 2)));
            //var valueAxis = new LogarithmicAxis{ MajorGridlineStyle = LineStyle.Solid, MinorGridlineStyle = LineStyle.Dot, Title = "Frequency (Hz)" };
            //tmp.Axes.Add(valueAxis);
            tmp.Series.Add(mySeries);
            tmp.Series.Add(medLine);

            if (!onebutton)
            {
                LineSeries mySeries2 = new LineSeries { StrokeThickness = 2, Color = OxyColors.Aqua, MarkerSize = 3, MarkerStroke = OxyColors.Crimson, MarkerType = MarkerType.Triangle };
                LineSeries medLine2 = new LineSeries { StrokeThickness = 1, Color = OxyColors.DarkBlue, LineStyle = LineStyle.Dash, MarkerType = MarkerType.None };
                test = 0;
                timestamp = 0;

                for (i = 0; i < mashes2.count; i++)
                {
                    test = mashes2.GetMash(i);
                    timestamp += test;
                    //Console.WriteLine(test);
                    mySeries2.Points.Add(new OxyPlot.DataPoint(Math.Round(timestamp/1000.0,3), Math.Round(1000.0 / test, 2)));
                    
                }
                medLine2.Points.Add(new OxyPlot.DataPoint(0.00, Math.Round(1000.0 / median2, 2)));
                medLine2.Points.Add(new OxyPlot.DataPoint(Math.Ceiling(timestamp/1000.00), Math.Round(1000.0 / median2, 2)));
                tmp.Series.Add(medLine2);
                tmp.Series.Add(mySeries2);

            }

            
            chart.Model = tmp;
            
            chart.Visibility = Visibility.Visible;
            chart.Model.DefaultYAxis.Title = "Frequency (Hz)";
            chart.Model.DefaultXAxis.Title = "Time (s)";
            chart.Model.DefaultYAxis.MajorStep = 2;
            chart.Model.DefaultYAxis.MinorStep = 1;
            chart.Model.DefaultYAxis.Minimum = 0;
            chart.Model.DefaultYAxis.Maximum = 20;
            
            chart.UpdateLayout();
            chart.IsEnabled = true;
            
            //myModel.
            //chart.
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            //myTimer.Enabled = true;
            //mystop.Start();
            //myTimer.Start();
            mashes = new MashSet();
            chart.Visibility = Visibility.Hidden;
            timerLabel.Visibility = Visibility.Visible;
            mashLabel.Visibility = Visibility.Visible;
            mashLabel.Content = "Mashes: 0";
            scoreLabel.Visibility = Visibility.Hidden;
            StartComms();

        }

        private void TimeOver(object sender, EventArgs e)
        {
            myTimer.Stop();
            updateTimer.Stop();
            timerLabel.Content = 0;
            timerLabel.Visibility = Visibility.Hidden; 
        }

        public void UpdateStats(MashSet newmashes, MashSet newmashes2)
        {

            mashes = newmashes;
            mashes2 = newmashes2;
            long mymed = 0;
            long mymed2 = 0;

            statusBar.Fill = Brushes.Red;
            if (onebutton)
            {
                avgTime.Content = FormatStrings((mashes.totalTime / 1000.0) / mashes.count);
                avgHz.Content = FormatStrings(mashes.count / (mashes.totalTime / 1000.0));
                avgDown.Content = FormatStrings(mashes.downTotal / mashes.count);
                avgUp.Content = FormatStrings(mashes.upTotal / mashes.count);
                avgDown2.Content = "";
                avgUp2.Content = "";
                maxTime.Content = mashes.fastest;
                minTime.Content = mashes.slowest;
                maxRate.Content = FormatStrings(1000.0 / (mashes.fastest));
                minRate.Content = FormatStrings(1000.0 / mashes.slowest);
                mymed = mashes.GetMedian();

                medRate.Content = FormatStrings(1000.0 / mymed);
                medTime.Content = mymed;

                
            }
            else
            {
                int totalmashes = mashes.count + mashes2.count;
                double totaltime = Math.Max(mashes.totalTime, mashes2.totalTime);
                long fastest = Math.Min(mashes.fastest, mashes2.fastest);
                long slowest = Math.Max(mashes.slowest, mashes2.slowest);

                avgTime.Content = FormatStrings((totaltime / 1000.0) / totalmashes);
                avgHz.Content = FormatStrings(totalmashes / (totaltime / 1000.0));
                avgDown.Content = FormatStrings(mashes.downTotal / mashes.count);
                avgUp.Content = FormatStrings(mashes.upTotal / mashes.count);
                avgDown2.Content = FormatStrings(mashes2.downTotal / mashes2.count);
                avgUp2.Content = FormatStrings(mashes2.upTotal / mashes2.count);
                maxTime.Content = fastest;
                minTime.Content = slowest;
                maxRate.Content = FormatStrings(1000.0 / fastest);
                minRate.Content = FormatStrings(1000.0 / slowest);
                mymed = mashes.GetMedian();

                medRate.Content = FormatStrings(1000.0 / mymed);

                mymed2 = mashes2.GetMedian();
                medTime.Content = FormatStrings(1000.0 / mymed2);
            }

            CalculateScore();

            PlotResults(mymed,mymed2);
        }

        private void CalculateScore()
        {

            double score = 0;
            double temp = 0;

            double mymed = 1000.0 / mashes.GetMedian();

            for (int i = 1; i < mashes.count; i++)
            {
                temp = 1000.0 / mashes.GetMash(i);
                temp = Math.Round(Math.Abs(temp - mymed)*10);
                score += 10 - Math.Min(10, temp);
            }
            
            if(!onebutton)
            {
                mymed = 1000.0 / mashes2.GetMedian();

                for (int i = 1; i < mashes2.count; i++)
                {
                    temp = 1000.0 / mashes2.GetMash(i);
                    temp = Math.Round(Math.Abs(temp - mymed) * 10);
                    score += 10 - Math.Min(10, temp);
                }
            }

            scoreLabel.Content = String.Format("Score: {0}",score);
            scoreLabel.Visibility = Visibility.Visible;
        }

        private string FormatStrings(double num)
        {
            return String.Format("{0:0.00}", num);
        }

        public void StatsInvoke(MashSet mymashes, MashSet mymashes2)
        {
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                   new Action(delegate ()
                                   {
                                       UpdateStats(mymashes, mymashes2);
                                   }));
        }

        public void MashIncrement(int val)
        {
            mashLabel.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                   new Action(delegate ()
                                   {
                                       mashLabel.Content = String.Format("Mashes: {0}",val);
                                   }));
        }

        private void comUpdate_Click(object sender, RoutedEventArgs e)
        {
            updatePortList();
        }

        private void playerUpdate_Click(object sender, RoutedEventArgs e)
        {
            usersBox.Items.Clear();
            List<string> users = sheets.GetUsernames();
            for (int i = 0; i < users.Count; i++)
            {
                usersBox.Items.Add(users[i]);
            }
            usersBox.SelectedIndex = 0;
        }
    }
}
