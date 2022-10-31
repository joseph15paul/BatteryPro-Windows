using System;
using Windows.Devices.Enumeration;
using Windows.Devices.Power;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BatteryPro
{
    public sealed partial class MainWindow : Window
    {
        ObservableCollection<BatteryStats> batterys = new();
        
        public MainWindow()
        {
            this.InitializeComponent();
            Battery.AggregateBattery.ReportUpdated += AggregateBattery_ReportUpdated;
           
        }
    
        private void RequestAggregateBatteryReport()
        {
            batterys = DataAccess.GetData();
            myDataGrid.ItemsSource = batterys;

        }

   


        private void AggregateBattery_ReportUpdated(Battery sender, object args)
        {
                _= DispatcherQueue.TryEnqueue(() => RequestAggregateBatteryReport());
        }

        private void AddData(object sender, RoutedEventArgs e)
        {
            if (myDataGrid.Visibility == Visibility.Collapsed)
            {
                chargeCycles.Visibility = Visibility.Collapsed;
                chargeCycleBtn.Content = "Charge Cycles";
                spotCount.Visibility = Visibility.Collapsed;
                optimalCount.Visibility = Visibility.Collapsed;
                badCount.Visibility = Visibility.Collapsed;
                chargePatternBtn.Content = "Charge Patterns";
                myDataGrid.Visibility = Visibility.Visible;
                myButton.Content = "Hide Data";
                batterys = DataAccess.GetData();
                myDataGrid.ItemsSource = batterys;
            }
            else
            {
                myDataGrid.Visibility = Visibility.Collapsed;
                myButton.Content = "View Data";

            }

        }

        private void calculateChargePatterns()
        {

            
            batterys = DataAccess.GetData();

            var optimalCountValue = 0;
            var badCountValue = 0;
            var spotCountValue = 0;
            var fullyCharged = false;
            var temp = batterys[0];
            var timeOfFullCharge = new DateTime();
            if (temp.batterylevel >99.75 && temp.isCharging)
            {
                fullyCharged = true;
                timeOfFullCharge = DateTime.ParseExact(temp.timeStamp, "yyyy:MM:dd HH:mm:ss:ffff", null);
            }
            batterys.RemoveAt(0);
            var elapsedTime = new TimeSpan();


            foreach (var i in batterys)
            {
                if (!fullyCharged)
                {
                    if (i.batterylevel >99.75 && i.isCharging && temp.batterylevel < 99.75)
                    {
                        fullyCharged = true;
                        timeOfFullCharge = DateTime.ParseExact(i.timeStamp, "yyyy:MM:dd HH:mm:ss:ffff", null);
                    }
                }
                if (i.isCharging && i.batterylevel > 99.75)
                {
                    elapsedTime += DateTime.ParseExact(i.timeStamp, "yyyy:MM:dd HH:mm:ss:ffff", null) - timeOfFullCharge;

                }
                else if (fullyCharged)
                {
                    if(i.batterylevel > 99.75)
                        elapsedTime += DateTime.ParseExact(i.timeStamp, "yyyy:MM:dd HH:mm:ss:ffff", null) - timeOfFullCharge;

                    if (elapsedTime.TotalMilliseconds <= 2 * 1000)
                    {
                        spotCountValue += 1;
                        Trace.WriteLine(elapsedTime.ToString());
                    }
                    else if (elapsedTime.TotalMilliseconds <= 30 * 60 * 1000)
                    {
                        optimalCountValue += 1;
                        Trace.WriteLine(elapsedTime.ToString());
                    }
                    else
                    {
                        badCountValue += 1;
                        Trace.WriteLine(elapsedTime.ToString());
                    }
                fullyCharged = false;
    
                }

                if (i.batterylevel < 99.75)
                {
                    fullyCharged = false;
                    elapsedTime = new();
                }
                temp = i;
            }
            spotCount.Text ="Spot Count: "+ spotCountValue.ToString();

            optimalCount.Text = "Optimal Count: " + optimalCountValue.ToString();
            badCount.Text = "Bad Count: " + badCountValue.ToString();
        }

            
        private ObservableCollection<ChargeCycle> calculateChargeCycles()
        {
            ObservableCollection<ChargeCycle> chargeCycleList = new();
            batterys = DataAccess.GetData();
            var initialBatterylevel = batterys[0].batterylevel;
            var initialTime = batterys[0].timeStamp;
            float dischargeLevel = 0f;
            float timeForDischarge = 0f;
            var temp = batterys[0];
            batterys.RemoveAt(0);

            var timeFrameStart = DateTime.ParseExact(initialTime, "yyyy:MM:dd HH:mm:ss:ffff", null);
            timeFrameStart = timeFrameStart.AddMinutes(-timeFrameStart.Minute);
            timeFrameStart = timeFrameStart.AddSeconds(-timeFrameStart.Second);
            timeFrameStart = timeFrameStart.AddMilliseconds(-timeFrameStart.Millisecond);



            foreach (var i in batterys)
            {
                //var elapsedTime = DateTime.ParseExact(i.timeStamp, "yyyy:MM:dd HH:mm:ss:ffff", null) - timeFrameStart;
                var startTime = DateTime.ParseExact(initialTime, "yyyy:MM:dd HH:mm:ss:ffff", null);
                var endTime = DateTime.ParseExact(i.timeStamp, "yyyy:MM:dd HH:mm:ss:ffff", null);

                if (timeFrameStart.Date == endTime.Date && timeFrameStart.Hour == endTime.Hour)
                {
                    var diff = endTime - startTime;
                    if (!i.isCharging)
                    {
                        dischargeLevel += initialBatterylevel - i.batterylevel;
                        timeForDischarge += (float)(diff.TotalMilliseconds);
                    }
                }
                else if ((endTime -timeFrameStart).TotalMinutes >= 120)
                {
                    for(int j=0;; j++)
                    {
                        string frame = timeFrameStart.ToShortDateString() + " " + timeFrameStart.ToShortTimeString() + " - " + timeFrameStart.AddHours(1).ToShortTimeString();
                        ChargeCycle chargeCycle = new();
                        chargeCycle.discharge = 0;
                        if(temp.isCharging)
                            chargeCycle.timeForDischarge = 0;
                        else
                            chargeCycle.timeForDischarge = 60;
                        chargeCycle.timeframe = frame;
                        chargeCycleList.Add(chargeCycle);

                        if (timeFrameStart.Date == endTime.Date && timeFrameStart.Hour == endTime.Hour)
                            break; 

                        timeFrameStart = timeFrameStart.AddHours(1);  //incremented an hour
                    }
                    if (!temp.isCharging)
                    {
                        if (!i.isCharging)
                            dischargeLevel = temp.batterylevel - i.batterylevel;
                        else
                            dischargeLevel = 0;
                        timeForDischarge = (float)(endTime - timeFrameStart).TotalMilliseconds;
                    }
                    else
                    {
                        dischargeLevel = 0;
                        timeForDischarge = 0;
                    }

                }
                else
                {
                    string frame = timeFrameStart.ToShortDateString() + " " + timeFrameStart.ToShortTimeString() + " - " + timeFrameStart.AddHours(1).ToShortTimeString();
                    timeFrameStart = timeFrameStart.AddHours(1);  //incremented an hour

                    if (!temp.isCharging)
                    {
                        var remainingTimeInTimeFrame = (timeFrameStart - DateTime.ParseExact(temp.timeStamp, "yyyy:MM:dd HH:mm:ss:ffff", null)).TotalMilliseconds;
                        timeForDischarge += (float)remainingTimeInTimeFrame;
                    }
                    
                    ChargeCycle chargeCycle = new();
                    chargeCycle.discharge = dischargeLevel;
                    chargeCycle.timeForDischarge =Convert.ToInt32(timeForDischarge / 60000);
                    chargeCycle.timeframe = frame;
                    chargeCycleList.Add(chargeCycle);
                    
                    if (!temp.isCharging)
                    {
                        if (!i.isCharging)
                            dischargeLevel = temp.batterylevel - i.batterylevel;
                        else
                            dischargeLevel = 0;
                        timeForDischarge = (float)(endTime - timeFrameStart).TotalMilliseconds;
                    }
                    else
                    {
                        dischargeLevel = 0;
                        timeForDischarge = 0;
                    }

                }

                initialTime = i.timeStamp;
                initialBatterylevel = i.batterylevel;
                temp = i;
            }
            return chargeCycleList;
        }


        private void viewChargeCycles(object sender, RoutedEventArgs e)
        {
            if (chargeCycles.Visibility == Visibility.Collapsed)
            {
                myDataGrid.Visibility = Visibility.Collapsed;
                myButton.Content = "View Data";
                chargeCycles.Visibility = Visibility.Visible;
                chargeCycleBtn.Content = "Hide Charge Cycles";
                spotCount.Visibility = Visibility.Collapsed;
                optimalCount.Visibility = Visibility.Collapsed;
                badCount.Visibility = Visibility.Collapsed;
                chargePatternBtn.Content = "Charge Patterns";
                chargeCycles.ItemsSource = calculateChargeCycles(); 
            }
            else
            {
                chargeCycles.Visibility = Visibility.Collapsed;
                chargeCycleBtn.Content = "Charge Cycles";

            }

        }



        private void viewChargePatterns(object sender, RoutedEventArgs e)
        {
            if (spotCount.Visibility == Visibility.Collapsed)
            {
                myDataGrid.Visibility = Visibility.Collapsed;
                myButton.Content = "View Data";
                chargeCycles.Visibility = Visibility.Collapsed;
                chargeCycleBtn.Content = "Charge Cycles";
                spotCount.Visibility = Visibility.Visible;
                optimalCount.Visibility = Visibility.Visible;
                badCount.Visibility = Visibility.Visible;
                chargePatternBtn.Content = "Hide Charge Patterns";
                calculateChargePatterns();


            }
            else
            {
                spotCount.Visibility = Visibility.Collapsed;
                optimalCount.Visibility = Visibility.Collapsed;
                badCount.Visibility = Visibility.Collapsed;
                chargePatternBtn.Content = "Charge Patterns";

            }

        }
    }
}
