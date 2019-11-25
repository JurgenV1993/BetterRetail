using System;
using System.Collections.Generic;
using System.Threading;


namespace Orckestra.Composer.CompositeC1.Services
{
    public class Scheduler: IScheduler
    {
        private Dictionary<string, Timer> timers = new Dictionary<string, Timer>();
    
        public void ScheduleTask(Action action, string name, int minutes)
        {
            if(timers.ContainsKey(name))
            {
                timers[name].Dispose();
                timers.Remove(name);
            }

            var autoEvent = new AutoResetEvent(false);

            var timer = new Timer(x =>
            {
                action();

                AutoResetEvent stateInfo = (AutoResetEvent)x;
                stateInfo.Set();

            },autoEvent, minutes * 60 * 1000, 0);
            timers.Add(name, timer);

            autoEvent.WaitOne();
            timers[name].Dispose();
            timers.Remove(name);
        }
    }
}
