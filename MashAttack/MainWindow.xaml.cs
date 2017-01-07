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
    public partial class AttackWindow : Window
    {
        PlayerForm subForm;

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
        const int PERIOD = 10;
        long downval = 0;
        long downval2 = 0;
        bool onebutton = true;
        SheetsAgent sheets;
        Label[,] statLabels = new Label[4, 6];
        bool relative = false;

        string player = "";
        string mode = "";
        string input = "";
        string duration = "";

        int button1 = 0;
        int button2 = 1;

        readonly SolidColorBrush BETTER = Brushes.Blue;
        readonly SolidColorBrush WORSE = Brushes.Red;
        readonly SolidColorBrush ZERO = Brushes.Black;

        Stats newStats = new Stats();
        Stats globalStats = new Stats();
        Stats playerStats = new Stats();
        //string comString;

        delegate void SetStatusCallback(string text);

        SerialComms serial;
        List<byte> _localBuffer;

        const byte SPINUP_SUCCESS = 128;
        const byte SPINUP_FAIL = 64;
        const byte ABORT = 0xFF;
        const byte START = 0xAA;

        //private delegate void UpdateStatsDel();

        public AttackWindow()
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

            //configBox.Items.Insert(0, "10s1B");
            //configBox.Items.Insert(1, "5s1B");
            //configBox.Items.Insert(2, "30s1B");

            //configBox.Items.Insert(3, "10s2B");
            //configBox.Items.Insert(4, "5s2B");
            //configBox.Items.Insert(5, "30s2B");

            //inputBox.Items.Insert(0, "SNES");
            //inputBox.Items.Insert(1, "NES");
            //inputBox.Items.Insert(2, "GEN");
            //inputBox.Items.Insert(3, "ARC");

            //configBox.SelectedIndex = 0;
            //inputBox.SelectedIndex = 0;

            int i = 0;
            int j = 0;

            for(i =0;i<4;i++)
                for(j = 0;j<6;j++)
                {
                    statLabels[i, j] = new Label();
                    statLabels[i, j].FontSize = 16;
                    statLabels[i, j].HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                    statLabels[i, j].VerticalAlignment = System.Windows.VerticalAlignment.Center;
                    statsGrid.Children.Add(statLabels[i, j]);
                    System.Windows.Controls.Grid.SetRow(statLabels[i, j], j);
                    System.Windows.Controls.Grid.SetColumn(statLabels[i, j], i);
                }

            statLabels[0, 1].Content = "Rate (Hz)";
            statLabels[0, 3].Content = "Uptime (ms)";
            statLabels[0, 4].Content = "Downtime (ms)";
            statLabels[0, 5].Content = "Score";
            statLabels[0, 2].Content = "Median (Hz)";

            statLabels[2, 0].Content = "Player";
            statLabels[1, 0].Content = "Session";
            statLabels[3, 0].Content = "Global";

            statLabels[0, 1].FontWeight = System.Windows.FontWeights.Bold;
            statLabels[0, 3].FontWeight = System.Windows.FontWeights.Bold;
            statLabels[0, 4].FontWeight = System.Windows.FontWeights.Bold;
            statLabels[0, 5].FontWeight = System.Windows.FontWeights.Bold;
            statLabels[0, 2].FontWeight = System.Windows.FontWeights.Bold;

            statLabels[1, 0].FontWeight = System.Windows.FontWeights.Bold;
            statLabels[2, 0].FontWeight = System.Windows.FontWeights.Bold;
            statLabels[3, 0].FontWeight = System.Windows.FontWeights.Bold;

            PlayerUpdate();
            UpdateButtonList();
        }

        void updatePortList()
        {
            comItem.Items.Clear();
            MenuItem newItem = new MenuItem();
            newItem.Header = "None";
            newItem.IsCheckable = true;
            newItem.IsChecked = true;
            newItem.Click += new RoutedEventHandler(COMSelect);
            comItem.Items.Add(newItem);
            foreach (string s in SerialPort.GetPortNames())
            {
                comItem.Items.Add(new MenuItem());
                newItem = (MenuItem)comItem.Items.GetItemAt(comItem.Items.Count - 1);
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

            for(int i = 0; i < comItem.Items.Count; i++)
            {
                tempItem = (MenuItem)comItem.Items.GetItemAt(i);
                tempItem.IsChecked = false;
            }

            tempItem = (MenuItem)sender;
            tempItem.IsChecked = true;
            comItem.Header = tempItem.Header.ToString();

            if(serial!=null)
                if (serial.isOpen())
                    serial.Close();

            try
            {
                if (tempItem.Header.ToString() != "None")
                {
                    serial = new SerialComms(tempItem.Header.ToString(), BAUD_RATE, PERIOD, ParseMessage, StatsInvoke);
                    //serial.StatusDelegate = Status;
                    //serial.BarDelegate = Bar;
                    //serial.CountdownDelegate = StartCountdown;
                    //serial.UpdateDelegate = StatsInvoke;
                    //serial.MashDelegate = MashIncrement;
                }
            }
            catch
            {
                Status("Failed to open Port! Is it already in use?");
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

            switch (mode)
            {
                case "Standard 1B":
                    serial.Command(10);
                    serial.Command(Config.GetCode(input, button1));
                    //onebutton = true;
                    countdown = 10;
                    break;
                case "Sprint 1B":
                    serial.Command(5);
                    serial.Command(Config.GetCode(input,button1));
                    //onebutton = true;
                    countdown = 5;
                    break;
                case "Marathon 1B":
                    serial.Command(30);
                    serial.Command(Config.GetCode(input, button1));
                    //onebutton = true;
                    countdown = 30;
                    break;
                case "Standard 2B":
                    serial.Command(10);
                    serial.Command(Config.GetCode(input, button1,button2));
                    //onebutton = false;
                    countdown = 10;
                    break;
                case "Sprint 2B":
                    serial.Command(5);
                    serial.Command(Config.GetCode(input, button1, button2));
                    //onebutton = false;
                    countdown = 5;
                    break;
                case "Marathon 2B":
                    serial.Command(30);
                    serial.Command(Config.GetCode(input, button1, button2));
                    //onebutton = false;
                    countdown = 30;
                    break;
                default:
                    serial.Command(10);
                    serial.Command(Config.GetCode(input, button1, button2));
                    //onebutton = true;
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
            statusLine.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                   new Action(delegate ()
                                   {
                                       //statusBox.AppendText(String.Format(v + "\n"));
                                       //statusBox.CaretIndex = statusBox.Text.Length;
                                       //statusBox.ScrollToEnd();
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

            if (onebutton)
            {
                medLine.Points.Add(new OxyPlot.DataPoint(0.00, Math.Round(1000.0 / median, 2)));
                medLine.Points.Add(new OxyPlot.DataPoint(Math.Ceiling(timestamp / 1000.00), Math.Round(1000.0 / median, 2)));
                tmp.Series.Add(medLine);
            }
            else
            {
                LineSeries medLine2 = new LineSeries { StrokeThickness = 1, Color = OxyColors.DarkBlue, LineStyle = LineStyle.Dash, MarkerType = MarkerType.None };
                double avg_median = 1000.00 / ((median + median2) / 2);
                double effective_median = 1000.00 / ((median + median2) / 4);

                medLine2.Points.Add(new OxyPlot.DataPoint(0.00, Math.Round(effective_median, 2)));
                medLine2.Points.Add(new OxyPlot.DataPoint(Math.Ceiling(timestamp / 1000.00), Math.Round(effective_median, 2)));
                medLine.Points.Add(new OxyPlot.DataPoint(0.00, Math.Round(avg_median, 2)));
                medLine.Points.Add(new OxyPlot.DataPoint(Math.Ceiling(timestamp / 1000.00), Math.Round(avg_median, 2)));
                tmp.Series.Add(medLine2);
                tmp.Series.Add(medLine);
            }

            

            chart.Model = tmp;
            
            chart.Visibility = Visibility.Visible;
            chart.Model.DefaultYAxis.Title = "Frequency (Hz)";
            chart.Model.DefaultXAxis.Title = "Time (s)";
            chart.Model.DefaultYAxis.MajorStep = 2;
            chart.Model.DefaultYAxis.MinorStep = 1;
            chart.Model.DefaultYAxis.Minimum = 0;
            chart.Model.DefaultYAxis.Maximum = 20;
            chart.Model.DefaultYAxis.TitleFontSize = 24;
            chart.Model.DefaultXAxis.TitleFontSize = 24;
            chart.Model.DefaultYAxis.FontSize = 18;
            chart.Model.DefaultXAxis.FontSize = 18;
            chart.Model.ResetAllAxes();
            
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
            //scoreLabel.Visibility = Visibility.Hidden;
            //winnerLabel.Content = " ";
            ClearStats();
            StartComms();

        }

        private void TimeOver(object sender, EventArgs e)
        {
            myTimer.Stop();
            updateTimer.Stop();
            timerLabel.Content = 0;
            //timerLabel.Visibility = Visibility.Hidden; 
        }

        public void UpdateStats(MashSet newmashes, MashSet newmashes2)
        {

            mashes = newmashes;
            mashes2 = newmashes2;
            long mymed = 0;
            long mymed2 = 0;

            Countdown(0);

            double score = CalculateScore();

            statusBar.Fill = Brushes.Red;
            if (onebutton)
            {
                mymed = mashes.GetMedian();

                newStats = new Stats(Math.Round(mashes.count/(mashes.totalTime/1000.0),3), mashes.upTotal/mashes.count, mashes.downTotal/mashes.count, score,Math.Round(1000.00/mymed,3));

            }
            else
            {
                double totalmashes = mashes.count + mashes2.count;
                double totaltime = Math.Max(mashes.totalTime, mashes2.totalTime);
                double totalup = mashes.upTotal + mashes2.upTotal;
                double totaldown = mashes.downTotal + mashes2.downTotal;

                mymed = mashes.GetMedian();

                mymed2 = mashes2.GetMedian();

                newStats = new Stats(Math.Round(totalmashes / (totaltime / 1000.0),3), Math.Round(totalup / totalmashes), Math.Round(totaldown / totalmashes), score, Math.Round(1000.00/((mymed+mymed2)/4),3));
            }

            sheets.SaveSession(newStats, usersBox.SelectedValue.ToString(), input, mode);
            playerStats = sheets.GetPlayer();
            globalStats = sheets.GetGlobal();

            CheckWinner();

            if (relative)
            {
                UpdateStatsRelative();
            }
            else
            {
                UpdateStatLabels();
            }

            timerLabel.Visibility = Visibility.Hidden;
            PlotResults(mymed,mymed2);
        }

        private void CheckWinner()
        {
            Status(newStats.max + "      " + playerStats.max + "      " + globalStats.max);
            if(Math.Round(globalStats.max,3) == Math.Round(newStats.max,3))
            {
                winnerLabel.Content = "New Rate Record!";
                winnerLabel.Foreground = Brushes.DarkGoldenrod;
            }
            else if(Math.Round(playerStats.max,3) == Math.Round(newStats.max,3))
            {
                winnerLabel.Content = "New Rate PB!";
                winnerLabel.Foreground = Brushes.Green;
            }

            if (Math.Round(globalStats.maxscore, 3) == Math.Round(newStats.maxscore, 3))
            {
                winner2Label.Content = "New Score Record!";
                winner2Label.Foreground = Brushes.DarkGoldenrod;
            }
            else if (Math.Round(playerStats.maxscore, 3) == Math.Round(newStats.maxscore, 3))
            {
                winner2Label.Content = "New Score PB!";
                winner2Label.Foreground = Brushes.Green;
            }
        }

        private void UpdateStatsRelative()
        {
            //Rate
            statLabels[2, 1].Content = GetDifference(newStats.rate,playerStats.rate) + "%";
            statLabels[1, 1].Content = FormatStrings(newStats.rate);
            statLabels[3, 1].Content = GetDifference(newStats.rate, globalStats.rate) + "%";

            //Median
            statLabels[2, 2].Content = GetDifference(newStats.median, playerStats.median) + "%";
            statLabels[1, 2].Content = FormatStrings(newStats.median);
            statLabels[3, 2].Content = GetDifference(newStats.median, globalStats.median) + "%";

            //Up
            statLabels[2, 3].Content = GetDifference(newStats.up, playerStats.up) + "%";
            statLabels[1, 3].Content = FormatStrings2(newStats.up);
            statLabels[3, 3].Content = GetDifference(newStats.up, globalStats.up) + "%";

            //Down
            statLabels[2, 4].Content = GetDifference(newStats.down, playerStats.down) + "%";
            statLabels[1, 4].Content = FormatStrings2(newStats.down);
            statLabels[3, 4].Content = GetDifference(newStats.down, globalStats.down) + "%";

            //Score
            statLabels[2, 5].Content = GetDifference(newStats.score, playerStats.score) + "%";
            statLabels[1, 5].Content = FormatStrings2(newStats.score);
            statLabels[3, 5].Content = GetDifference(newStats.score, globalStats.score) + "%";

            UpdateColors();
        }

        private void UpdateColors()
        {
                //Rate
                statLabels[2, 1].Foreground = GetColorHigh(newStats.rate, playerStats.rate);
                statLabels[3, 1].Foreground = GetColorHigh(newStats.rate, globalStats.rate);

                //Median
                statLabels[2, 2].Foreground = GetColorHigh(newStats.median, playerStats.median);
                statLabels[3, 2].Foreground = GetColorHigh(newStats.median, globalStats.median);

                //Up
                statLabels[2, 3].Foreground = GetColorLow(newStats.up, playerStats.up);
                statLabels[3, 3].Foreground = GetColorLow(newStats.up, globalStats.up);

                //Down
                statLabels[2, 4].Foreground = GetColorLow(newStats.down, playerStats.down);
                statLabels[3, 4].Foreground = GetColorLow(newStats.down, globalStats.down);

                //Score
                statLabels[2, 5].Foreground = GetColorHigh(newStats.score, playerStats.score);
                statLabels[3, 5].Foreground = GetColorHigh(newStats.score, globalStats.score);
        }

        private double GetDifference(double val1, double val2)
        {
            return Math.Round(((val1 - val2)/val2)*100,1);
        }

        private SolidColorBrush GetColorHigh(double val1, double val2)
        {
            double myval = Math.Floor(((val1 - val2) / val2) * 100);

            if (myval > 0)
                return BETTER;
            else if (myval < 0)
                return WORSE;
            else
                return ZERO;
        }

        private SolidColorBrush GetColorLow(double val1, double val2)
        {
            double myval = Math.Floor(((val1 - val2) / val2) * 100);

            if (myval > 0)
                return WORSE;
            else if (myval < 0)
                return BETTER;
            else
                return ZERO;
        }

        private void UpdateStatLabels()
        {
            //Rate
            statLabels[2, 1].Content = FormatStrings(playerStats.rate);
            statLabels[1, 1].Content = FormatStrings(newStats.rate);
            statLabels[3, 1].Content = FormatStrings(globalStats.rate);

            //Median
            statLabels[2, 2].Content = FormatStrings(playerStats.median);
            statLabels[1, 2].Content = FormatStrings(newStats.median);
            statLabels[3, 2].Content = FormatStrings(globalStats.median);

            //Up
            statLabels[2, 3].Content = FormatStrings2(playerStats.up);
            statLabels[1, 3].Content = FormatStrings2(newStats.up);
            statLabels[3, 3].Content = FormatStrings2(globalStats.up);

            //Down
            statLabels[2, 4].Content = FormatStrings2(playerStats.down);
            statLabels[1, 4].Content = FormatStrings2(newStats.down);
            statLabels[3, 4].Content = FormatStrings2(globalStats.down);

            //Score
            statLabels[2, 5].Content = FormatStrings2(playerStats.score);
            statLabels[1,5].Content = FormatStrings2(newStats.score);
            statLabels[3, 5].Content = FormatStrings2(globalStats.score);

            UpdateColors();
        }

        private void ClearStats()
        {
            int i, j;

            for(i=1;i<4;i++)
                for (j = 1; j < 6; j++)
                {
                    statLabels[i, j].Content = "";
                }

            winnerLabel.Content = " ";
            scoreLabel.Content = " ";
            winner2Label.Content = " ";
        }

        private double CalculateScore()
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

            //Stats session = new MashAttack.Stats((long)mashes.totalTime, mashes.count, (long)mashes.upTotal, (long)mashes.downTotal, 1, (long)score);
            //Console.WriteLine(usersBox.SelectedValue.ToString());
            //Console.WriteLine(configBox.SelectedValue.ToString());
            //session.UpdateAll(usersBox.SelectedValue.ToString(), configBox.SelectedValue.ToString(), "SNES");

            if (!onebutton)
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
            //scoreLabel.Visibility = Visibility.Visible;

            return score;
        }

        private string FormatStrings(double num)
        {
            return String.Format("{0:0.00}", num);
        }

        private string FormatStrings2(double num)
        {
            return String.Format("{0:0}", num);
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
            PlayerUpdate();
        }

        private void PlayerUpdate()
        {
            usersBox.Items.Clear();
            List<string> users = sheets.GetUsernames();
            for (int i = 0; i < users.Count; i++)
            {
                usersBox.Items.Add(users[i]);
            }
            usersBox.SelectedIndex = 0;
        }

        private void relativeItem_Click(object sender, RoutedEventArgs e)
        {
            relative = !relative;

            if (relative)
            {
                UpdateStatsRelative();
            }
            else
            {
                UpdateStatLabels();
            }
        }

        private void ParseMessage(int code, string message)
        {
            switch (code)
            {
                case SerialComms.STATUS:
                    Status(message);
                    break;
                case SerialComms.ACTIVE:
                    Status(message);
                    Bar(Brushes.Yellow);
                    break;
                case SerialComms.MASH:
                    MashIncrement(Convert.ToInt32(message));
                    break;
                case SerialComms.STARTED:
                    Status(message);
                    StartCountdown();
                    break;
                default:
                    Status(message);
                    break;
            }
                    
        }

        private void duration_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton temp= (RadioButton)sender;
            duration = temp.Content.ToString();

            mode = duration + (onebutton ? " 1B" : " 2B");
        }

        private void input_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton temp = (RadioButton)sender;
            input = temp.Content.ToString();
            UpdateButtonList();
        }

        private void UpdateButtonList()
        {
            List<string> items;
            if ((bool)snesRadio.IsChecked)
            {
                items = Config.buttons["SNES"];
            }
            else if ((bool)nesRadio.IsChecked)
            {
                items = Config.buttons["NES"];
            }
            else if ((bool)genRadio.IsChecked)
            {
                items = Config.buttons["GEN"];
            }
            else
            {
                items = Config.buttons["ARC"];
            }

            button1Menu.Items.Clear();
            button2Menu.Items.Clear();
            MenuItem tempitem;

            for(int i = 0; i< items.Count; i++)
            {
                button1Menu.Items.Add(new MenuItem());
                tempitem = (MenuItem)button1Menu.Items[i];
                tempitem.IsCheckable = true;
                tempitem.Click += ButtonsChanged;
                tempitem.Header = items[i];

                button2Menu.Items.Add(new MenuItem());
                tempitem = (MenuItem)button2Menu.Items[i];
                tempitem.IsCheckable = true;
                tempitem.Click += ButtonsChanged;
                tempitem.Header = items[i];
            }

            tempitem = (MenuItem)button1Menu.Items[0];
            tempitem.IsChecked = true;
            button1Menu.Header = tempitem.Header;

            tempitem = (MenuItem)button2Menu.Items[1];
            tempitem.IsChecked = true;
            button2Menu.Header = tempitem.Header;

            button1 = 0;
            button2 = 1;
        }

        private void ButtonsChanged(object sender, RoutedEventArgs e)
        {
            MenuItem tempitem = (MenuItem)sender;
            MenuItem parentitem = (MenuItem)tempitem.Parent;

            for(int i = 0; i < parentitem.Items.Count; i++)
            {
                tempitem = (MenuItem)parentitem.Items[i];
                tempitem.IsChecked = false;
            }

            tempitem = (MenuItem)sender;
            tempitem.IsChecked = true;

            int val1 = parentitem.Items.IndexOf(sender);

            tempitem = (MenuItem)button1Menu.Items[val1];

            if (tempitem.IsChecked)
            {
                tempitem = (MenuItem)button2Menu.Items[val1];
                if (tempitem.IsChecked)
                {
                    tempitem.IsChecked = false;
                    int val2 = val1+1;

                    if (val2 == button2Menu.Items.Count)
                        val2 = 0;

                    button1Menu.Header = tempitem.Header;

                    tempitem = (MenuItem)button2Menu.Items[val2];
                    tempitem.IsChecked = true;
                    button2Menu.Header = tempitem.Header;

                    button1 = val1;
                    button2 = val2;

                    return;
                }
            }
            tempitem = (MenuItem)parentitem.Items[val1];

            parentitem.Header = tempitem.Header;

            if (parentitem == button1Menu)
                button1 = val1;
            else
                button2 = val1;
        }

        private void twobuttonCheck_Click(object sender, RoutedEventArgs e)
        {
            onebutton = !onebutton;

            mode = duration + (onebutton ? " 1B" : " 2B");
        }

        private void ledItem_Click(object sender, RoutedEventArgs e)
        {
            if(serial!= null)
                if(serial.isOpen())
                    serial.Command(SerialComms.LEDS);
        }

        private void newplayerItem_Click(object sender, RoutedEventArgs e)
        {
            if (subForm == null || !subForm.IsLoaded)
            {
                subForm = new PlayerForm(this);
                subForm.Show();
            }
        }

        public void AddPlayer(string newname)
        {
            sheets.AddUsername(newname);
            PlayerUpdate();
            usersBox.Text = newname;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (subForm != null)
                subForm.Close();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            banner.Source = new BitmapImage(new Uri("C:\\MashAttack\\mash-01.png", UriKind.Absolute));
        }
    }
}
