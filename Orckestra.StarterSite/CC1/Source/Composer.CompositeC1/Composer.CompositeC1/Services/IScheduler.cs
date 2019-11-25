using System;

namespace Orckestra.Composer.CompositeC1.Services
{
    public interface IScheduler
    {
        void ScheduleTask(Action action, string name, int minutes);
    }
}
