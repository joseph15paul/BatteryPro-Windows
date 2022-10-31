
using Windows.Devices.Power;


namespace BatteryStatsCollectionWorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            DataAccess.InitializeDatabase();
            Battery.AggregateBattery.ReportUpdated += AggregateBattery_ReportUpdated;

            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //try
            //{
            //    while (!stoppingToken.IsCancellationRequested)
            //    {
            //        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            //        await Task.Delay(60000, stoppingToken);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "{Message}", ex.Message);

            //    // Terminates this process and returns an exit code to the operating system.
            //    // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
            //    // performs one of two scenarios:
            //    // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
            //    // 2. When set to "StopHost": will cleanly stop the host, and log errors.
            //    //
            //    // In order for the Windows Service Management system to leverage configured
            //    // recovery options, we need to terminate the process with a non-zero exit code.
            //    Environment.Exit(1);
            //}
        }
 
        private void AggregateBattery_ReportUpdated(Battery sender, object args)
        {
            // Create aggregate battery object
            var aggBattery = Battery.AggregateBattery;

            // Get report
            var report = aggBattery.GetReport();

            AddData(report);
        }

        private void AddData(BatteryReport report)
        {
            string? batteryLevel = (Convert.ToDouble(report.RemainingCapacityInMilliwattHours) / Convert.ToDouble(report.FullChargeCapacityInMilliwattHours) * 100).ToString("F2");
            bool isCharging;

            if (report.Status.ToString() == "Discharging")
                isCharging = false;
            else
                isCharging = true;

            string GetTimestamp(DateTime value)
            {
                return value.ToString("yyyy:MM:dd HH:mm:ss:ffff");
            }

            string timeStamp = GetTimestamp(DateTime.Now);


            DataAccess.AddData(batteryLevel, isCharging, timeStamp);

           
        }
    }
}