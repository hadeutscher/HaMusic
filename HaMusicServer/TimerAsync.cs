using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaMusicServer
{
    public class TimerAsync
    {
        private Action timerAction;
        private int timerInterval;

        public TimerAsync(Action action, int interval)
        {
            timerAction = action;
            timerInterval = interval;
        }

        public async void Run()
        {
            while (true)
            {
                await Task.Delay(timerInterval);
                timerAction();
            }
        }
    }
}
