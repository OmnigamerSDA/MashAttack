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


namespace MashAttack
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DispatcherTimer myTimer;
        Timer updateTimer;
        bool mashEnabled;
        MashSet mashes;
        bool first;
        System.Diagnostics.Stopwatch mystop;
        long prior;
        long current;
        long release;
        bool isDown;
        bool isUp;
        int countdown;
        public PlotModel myModel;

        //private delegate void UpdateStatsDel();

        public MainWindow()
        {
            InitializeComponent();
            //OxyPlot.Model
            myTimer = new DispatcherTimer(DispatcherPriority.Normal);
            myTimer.Interval = new TimeSpan(0, 0, 0, 10, 0) ;
            myTimer.Tick += new EventHandler(TimeOver);
            updateTimer = new Timer();
            updateTimer.Interval = 1000;
            updateTimer.Tick += new EventHandler(SecondElapsed);
            //myTimer.AutoReset = false;
            //myTimer.SynchronizingObject = (System.ComponentModel.ISynchronizeInvoke)this.avgHz;
            mashEnabled = false;
            mashes = new MashSet();
            first = true;
            mystop = new System.Diagnostics.Stopwatch();
            prior = 0;
            //myModel = new PlotModel { Title = "Mashing Rate" };
        }

        private void SecondElapsed(object sender, EventArgs e)
        {
            timerLabel.Content = String.Format("{0}", countdown--);
        }

        private void PlotResults()
        {
            PlotModel tmp = new PlotModel { Title = "Mash Rate" };
            LineSeries mySeries = new LineSeries { StrokeThickness = 2, Color=OxyColors.PaleVioletRed, MarkerSize = 3, MarkerStroke = OxyColors.ForestGreen, MarkerType = MarkerType.Plus };
            //myModel.Clear();
            int i;
            long test = 0;
            for (i = 0; i < mashes.count; i++)
            {
                test = mashes.GetMash(i);
                Console.WriteLine(test);
                mySeries.Points.Add(new OxyPlot.DataPoint(i, test));
                
            }

            tmp.Series.Add(mySeries);
            chart.Model = tmp;
            chart.UpdateLayout();
            chart.Visibility = Visibility.Visible;
            chart.IsEnabled = true;
            
            //myModel.
            //chart.
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //myTimer.Enabled = true;
            //mystop.Start();
            //myTimer.Start();
            mashEnabled = true;
            countdown = 9;
            statusBar.Fill = Brushes.Yellow;
            startButton.IsEnabled = false;
            isDown = false;
            isUp = true;
        }

        private void TimeOver(object sender, EventArgs e)
        {
            myTimer.Stop();
            
            mystop.Stop();
            timeLabel.Content = mystop.ElapsedMilliseconds;
            mystop.Reset();
            mashEnabled = false;
            first = true;
            isUp = true;
            isDown = false;
            startButton.IsEnabled = true;
            updateTimer.Stop();
            timerLabel.Content = 0;

            UpdateStats();
        }

        private void UpdateStats()
        {
            statusBar.Fill = Brushes.Red;
            avgTime.Content = FormatStrings((mashes.totalTime / 1000.0) / mashes.count);
            avgHz.Content = FormatStrings(mashes.count / (mashes.totalTime/1000.0));
            avgDown.Content = FormatStrings(mashes.downTotal / mashes.count);
            avgUp.Content = FormatStrings(mashes.upTotal / mashes.count);
            maxTime.Content = mashes.fastest;
            minTime.Content = mashes.slowest;
            maxRate.Content = FormatStrings(1000.0 / (mashes.fastest));
            minRate.Content = FormatStrings(1000.0 / mashes.slowest);
            long mymed = mashes.GetMedian();

            medRate.Content = 1000.0 / mymed;
            medTime.Content = mymed;

            PlotResults();
        }

        private string FormatStrings(double num)
        {
            return String.Format("{0:0.00}", num);
        }

        private void Grid_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (mashEnabled && !isDown)
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
            if (mashEnabled && !isUp)
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
    }
}
