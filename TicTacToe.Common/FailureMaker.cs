using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicTacToe.Common
{
    public class FailureMaker
    {
        private System.Timers.Timer? _timer;

        static Random _random = new Random();

        public static TimeSpan MinWorkTime = TimeSpan.FromSeconds(60);

        public static TimeSpan RandomDelta = TimeSpan.FromSeconds(10 * 60);

        public static TimeSpan GetFailurePoint() => TimeSpan.FromSeconds(MinWorkTime.TotalSeconds + RandomDelta.TotalSeconds * _random.NextDouble());

        public void PutBomb()
        {
            _timer = new System.Timers.Timer();
            _timer.Elapsed += (s, ea) => Environment.Exit(1);
            _timer.Interval = GetFailurePoint().TotalMilliseconds;
            _timer.AutoReset = false;
            _timer.Start();
        }
    }
}
