using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
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

        delegate void SetStatusCallback(string text);

        SerialPort _datPort;
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
            comBox.Items.Clear();
            foreach (string s in SerialPort.GetPortNames())
            {
                comBox.Items.Add(s);
            }
            comBox.SelectedIndex = comBox.Items.Count - 1;
        }

        private void SerialComms()
        {
            _datPort.DiscardOutBuffer();//Flush the output buffer
            _datPort.DiscardInBuffer();

            Command(START);//Write out control data to Arduino
            Status("Start signal sent.");

            switch (configBox.SelectedIndex)
            {
                case 0:
                    Command(10);
                    Command(0xC0);
                    onebutton = true;
                    countdown = 10;
                    break;
                case 1:
                    Command(5);
                    Command(0xC0);
                    onebutton = true;
                    countdown = 5;
                    break;
                case 2:
                    Command(30);
                    Command(0xC0);
                    onebutton = true;
                    countdown = 30;
                    break;
                case 3:
                    Command(10);
                    Command(0xC1);
                    onebutton = false;
                    countdown = 10;
                    break;
                case 4:
                    Command(5);
                    Command(0xC1);
                    onebutton = false;
                    countdown = 5;
                    break;
                case 5:
                    Command(30);
                    Command(0xC1);
                    onebutton = false;
                    countdown = 30;
                    break;
                default:
                    Command(10);
                    Command(0xC0);
                    onebutton = true;
                    countdown = 10;
                    break;
            }
        }

        private void StartCountdown()
        {
            //mystop.Start();
            myTimer.Interval = new TimeSpan(0, 0, 0, countdown, 0);
            myTimer.Start();
            updateTimer.Start();
            Countdown(countdown);
            Bar(Brushes.Green);
        }

        private bool Command(byte opcode)
        {
            if (_datPort == null || !_datPort.IsOpen) return false;
            byte[] inputs = new byte[1];

            inputs[0] = opcode;

            _datPort.Write(inputs, 0, 1);//Write out control data to Arduino
            Status(String.Format("Wrote command: {0:x}", opcode));
            return true ;
        }

        void tick(object sender, EventArgs e)
        {
            if (_datPort == null || !_datPort.IsOpen) return;
            byte[] readBuffer = new byte[3];
            int readCount = 0;

            // Try to read some data from the COM port and append it to our localBuffer.
            // If there's an IOException then the device has been disconnected.
            readCount = _datPort.BytesToRead;
            while (readCount > 2)
            {

                try
                {
                    //Status(String.Format("{0}", readCount));
                    _datPort.Read(readBuffer, 0, 3); //Read in 3 bytes from the com port
                    //Status(String.Format("Mash {0}: {1:d} {2:d}", mashes.count, readBuffer[1], readBuffer[2]));
                }
                catch (IOException)
                {
                    Status("Something went wrong.");
                    return;
                }

                ParseResponse(readBuffer);

                readCount = _datPort.BytesToRead; //update read count in case multiple comms occurred
            }
        }

        private int TimedRead(int interval)
        {
            Timer statusTimer = new Timer();
            statusTimer.Interval = interval;
            statusTimer.Tick += new EventHandler(TimeOutCheck);
            statusTimer.Start();
            timeout = false;
            int readCount = 0;
            do
            {
                readCount = _datPort.BytesToRead;
            } while (readCount <= 0 && !timeout);

            statusTimer.Stop();

            if (timeout)
            {
                timeout = false;
                Status("Comms timeout!");
                return -1;
            }
            else
                return readCount;
        }

        private void TimeOutCheck(object sender, EventArgs e)
        {
            timeout = true;
        }

        private void Status(string v)
        {
            statusBox.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                   new Action(delegate ()
                                   {
                                       statusBox.AppendText(String.Format(v + "\n"));
                                       statusBox.CaretIndex = statusBox.Text.Length;
                                       statusBox.ScrollToEnd();
                                   }));

        }

        private void Bar(Brush mycolor)
        {
            statusBar.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                   new Action(delegate ()
                                   {
                                       statusBar.Fill = mycolor;
                                   }));
        }

        private void Countdown(int num)
        {
            timerLabel.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                   new Action(delegate ()
                                   {
                                       timerLabel.Content = num;
                                   }));
        }

        private bool ParseResponse(byte[] response)
        {
            switch (response[0])
            {
                case Commands.SPINUP:
                    Status("Device Ready.\n");
                    Status(String.Format("Seconds: {0}    Config: {1:x}", response[1], response[2]));
                    //countdown = 9;
                    Bar(Brushes.Yellow);
                    mashes = new MashSet();
                    mashes2 = new MashSet();
                    //isDown = true;
                    return true;
                case Commands.INITIATED:
                    Status("First mash started.");
                    StartCountdown();
                    return true;
                case Commands.DOWNTIME:
                    //Status("Down Received.");
                    downval = (long)(response[1] + (response[2] * 256));
                    return true;
                case Commands.UPTIME:
                    //Status("Up Received.");
                    CaptureMash(response[1], response[2]);
                    return true;
                case Commands.DOWNTIME2:
                    //Status("Down2 Received.");
                    downval2 = (long)(response[1] + (response[2] * 256));
                    return true;
                case Commands.UPTIME2:
                    //Status("Up2 Received.");
                    CaptureMash2(response[1], response[2]);
                    return true;
                case Commands.FINISHED:
                    Status("Session finished.\n");
                    this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                   new Action(delegate ()
                                   {
                                       UpdateStats();
                                   }));
                    return true;
                default:
                    Status("Device Comms Error. \n");
                    return false;
            }
        }

        private void CaptureMash(byte v1, byte v2)
        {

            long upval = (long)(v1 + (v2 * 256));
            mashes.AddMash(downval/PERIOD, upval/PERIOD);
            Status(String.Format("Added Mash {0}: {1} {2}", mashes.count - 1, downval, upval));
            timeLabel.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                   new Action(delegate ()
                                   {
                                       timeLabel.Content = mashes.count + mashes2.count;
                                   }));
        }

        private void CaptureMash2(byte v1, byte v2)
        {

            long upval = (long)(v1 + (v2 * 256));
            mashes2.AddMash(downval2 / PERIOD, upval / PERIOD);
            Status(String.Format("Added Mash2 {0}: {1} {2}", mashes2.count - 1, downval2, upval));
            timeLabel.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                   new Action(delegate ()
                                   {
                                       timeLabel.Content = mashes.count + mashes2.count;
                                   }));
        }

        private void SecondElapsed(object sender, EventArgs e)
        {
            Countdown(--countdown);
            //Status("Timer updated");
        }

        private void PlotResults()
        {
            PlotModel tmp = new PlotModel { Title = "Mash Rate" };
            
            LineSeries mySeries = new LineSeries { StrokeThickness = 2, Color=OxyColors.PaleVioletRed, MarkerSize = 3, MarkerStroke = OxyColors.ForestGreen, MarkerType = MarkerType.Plus };
            //myModel.Clear();
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
            //var valueAxis = new LogarithmicAxis{ MajorGridlineStyle = LineStyle.Solid, MinorGridlineStyle = LineStyle.Dot, Title = "Frequency (Hz)" };
            //tmp.Axes.Add(valueAxis);
            tmp.Series.Add(mySeries);

            if (!onebutton)
            {
                LineSeries mySeries2 = new LineSeries { StrokeThickness = 2, Color = OxyColors.Aqua, MarkerSize = 3, MarkerStroke = OxyColors.Crimson, MarkerType = MarkerType.Triangle };
                test = 0;
                timestamp = 0;

                for (i = 0; i < mashes2.count; i++)
                {
                    test = mashes2.GetMash(i);
                    timestamp += test;
                    //Console.WriteLine(test);
                    mySeries2.Points.Add(new OxyPlot.DataPoint(Math.Round(timestamp/1000.0,3), Math.Round(1000.0 / test, 2)));
                    
                }
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
            
            if (kb_mode)
            {
                mashEnabled = true;

                startButton.IsEnabled = false;
                isDown = false;
                isUp = true;
                countdown = 9;
                statusBar.Fill = Brushes.Yellow;
            }
            else
            {
                SerialComms();
            }
        }

        private void TimeOver(object sender, EventArgs e)
        {
            myTimer.Stop();
            updateTimer.Stop();
            timerLabel.Content = 0;

            if (kb_mode)
            {
                mystop.Stop();
                timeLabel.Content = mystop.ElapsedMilliseconds;
                mystop.Reset();
                mashEnabled = false;
                first = true;
                isUp = true;
                isDown = false;
                startButton.IsEnabled = true;
                UpdateStats();
            }
            else
            {
                //LoadMashes();

            }

            
        }

        private void LoadMashes()
        {
            byte[] response = new byte[1];
            byte[] mashnum = new byte[2];
            int total_mashes = 0;

            if(TimedRead(2000)>0)
            {
                _datPort.Read(response, 0, 1); //Read in bytes from the com port

                if (ParseResponse(response))
                {
                    _datPort.Read(mashnum, 0, 2); //Read in bytes from the com port

                    total_mashes = mashnum[0] + mashnum[1] * 256;

                    byte[] downvals = new byte[total_mashes * 2];
                    byte[] upvals = new byte[total_mashes * 2];

                    _datPort.Read(downvals, 0, total_mashes * 2);
                    _datPort.Read(upvals, 0, total_mashes * 2);

                    long[] downs = Convert_Shorts(downvals, total_mashes);
                    long[] ups = Convert_Shorts(upvals, total_mashes);

                    for(int i = 0; i < total_mashes; i++)
                    {
                        mashes.AddMash(downs[i], ups[i]);
                    }
                }

            }
        }

        private long[] Convert_Shorts(byte[] bytevals, int total_mashes)
        {
            long[] results = new long[total_mashes];
            for(int i = 0; i < total_mashes; i++)
            {
                results[i] = bytevals[i*2] + bytevals[i * 2 + 1] * 256;
            }

            return results;
        }

        private void UpdateStats()
        {
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
                long mymed = mashes.GetMedian();

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
                long mymed = mashes.GetMedian();

                medRate.Content = FormatStrings(1000.0 / mymed);

                mymed = mashes2.GetMedian();
                medTime.Content = FormatStrings(1000.0 / mymed);
            }

            PlotResults();
        }

        private string FormatStrings(double num)
        {
            return String.Format("{0:0.00}", num);
        }

        private void Grid_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (mashEnabled && !isDown && kb_mode)
            {
                if(e.Key == Key.NumPad8)
                {
                    if (!first)
                    {
                        current = mystop.ElapsedMilliseconds;

                        mashes.AddMash(prior, release, current);

                        prior = current;
                        isDown = true;
                        isUp = false;
                    }
                    else
                    {
                        mystop.Start();
                        myTimer.Start();
                        prior = mystop.ElapsedMilliseconds;
                        updateTimer.Start();
                        timerLabel.Content = 10;
                        statusBar.Fill = Brushes.Green;
                        first = false;
                        isDown = true;
                        isUp = false;
                    }
                        
                }
            }
        }

        private void Grid_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (mashEnabled && !isUp && kb_mode)
            {
                if (e.Key == Key.NumPad8)
                {
                    release = mystop.ElapsedMilliseconds;
                    isUp = true;
                    isDown = false;
                }
            }
        }

        private void updateButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateStats();
        }

        private void plotButton_Click(object sender, RoutedEventArgs e)
        {
            PlotResults();
        }

        private void connectButton_Click(object sender, RoutedEventArgs e)
        {

            _datPort = new SerialPort(comBox.Text, BAUD_RATE);//set up the serial port
            //_datPort.ReadBufferSize = 65536;
            if (!_datPort.IsOpen)//Open, if not already
                _datPort.Open();

            if (_datPort == null || !_datPort.IsOpen)
            {
                Status("Could not open Port!\n");
                return;
            }

            _datPort.DtrEnable = false;
            _datPort.RtsEnable = false;
            

            _datPort.ReceivedBytesThreshold = 3;
            _datPort.DataReceived += new SerialDataReceivedEventHandler(tick);

            Status(String.Format("Successfully connected to {0}", comBox.Text));

        }
    }
}
