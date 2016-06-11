using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Sensors;

namespace my_home_pi
{
    class Startup
    {
        public static async void LaunchBme280LogTask()
        {
            //var accelerometer = Accelerometer.GetDefault();
            //BackgroundAccessStatus accessStatus = await BackgroundExecutionManager.RequestAccessAsync();
            //var trigger = new DeviceServicingTrigger();
            //await trigger.RequestAsync(accelerometer.DeviceId, );
            var task = RegisterBackgroundTask(
                "bme280_service.Bme280LogService",
                "Bme280LogTask",
                new SystemTrigger(SystemTriggerType.TimeZoneChange, true),
                //new TimeTrigger(15, true),
                //new DeviceUseTrigger(),
                null);
            task.Progress += Task_Progress;
            task.Completed += Task_Completed;
        }

        private static void Task_Progress(BackgroundTaskRegistration sender, BackgroundTaskProgressEventArgs args)
        {
            Debug.WriteLine(args);
        }

        private static void Task_Completed(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            Debug.WriteLine(args);
        }

        // https://msdn.microsoft.com/ja-jp/windows/uwp/launch-resume/register-a-background-task
        public static BackgroundTaskRegistration RegisterBackgroundTask(string taskEntryPoint,
                                                                   string taskName,
                                                                   IBackgroundTrigger trigger,
                                                                   IBackgroundCondition condition)
        {
            // Check for existing registrations of this background task.
            foreach (var cur in BackgroundTaskRegistration.AllTasks)
            {
                if (cur.Value.Name == taskName)
                {
                    // The task is already registered.
                    //return (BackgroundTaskRegistration)(cur.Value);
                    // XXX Refresh
                    cur.Value.Unregister(true);
                }
            }

            // Register the background task.
            var builder = new BackgroundTaskBuilder();

            builder.Name = taskName;
            builder.TaskEntryPoint = taskEntryPoint;
            builder.SetTrigger(trigger);

            if (condition != null)
            {
                builder.AddCondition(condition);
            }

            BackgroundTaskRegistration task = builder.Register();

            return task;
        }
    }
}
