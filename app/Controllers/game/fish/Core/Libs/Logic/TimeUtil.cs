using System;

namespace BanCa.Libs
{
    public class TimeUtil
    {
#if SERVER
        private const int BUFF_SIZE = 4;
#else
        private const int BUFF_SIZE = 10;
#endif
        private static int _delayIndex = 0;
        private static long[] _delayBuf = new long[BUFF_SIZE];
        private static int _timeIndex = 0;
        private static long[] _timeBuf = new long[BUFF_SIZE];
        private static bool delayInited = false;
        private static bool timeInited = false;

        public const float PREDICT_TIME = 1f;

        public static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1);
        public static long TimeStamp
        {
            get
            {
                return (long)(DateTime.UtcNow.Subtract(UnixEpoch)).TotalMilliseconds;
            }
        }

        private static long delayMs = 0;
        private static long timeDiffMs = 0;
#if SERVER
        private static object _lock = new object();
#endif

        public static long DelayMs
        {
            get
            {
                return delayMs;
            }
            set
            {
#if SERVER
                lock (_lock)
                {
#endif
                    if (delayInited)
                    {
                        _delayBuf[_delayIndex] = value;
                        _delayIndex++;
                        if (_delayIndex >= BUFF_SIZE)
                        {
                            _delayIndex = 0;
                        }
                        var total = 0L;
                        for (int i = 0; i < BUFF_SIZE; i++)
                        {
                            total += _delayBuf[i];
                        }
                        delayMs = total / BUFF_SIZE;
                    }
                    else
                    {
                        delayInited = true;
                        var delay = value;
                        for (int i = 0; i < BUFF_SIZE; i++)
                        {
                            _delayBuf[i] = delay;
                        }
                        delayMs = delay;
                    }
#if SERVER
                }
#endif
            }
        }
        public static long ClientServerTimeDifferentMs
        {
            get
            {
                return timeDiffMs;
            }
            set
            {
#if SERVER
                lock (_lock)
                {
#endif
                    if (timeInited)
                    {
                        _timeBuf[_timeIndex] = value;
                        _timeIndex++;
                        if (_timeIndex >= BUFF_SIZE)
                        {
                            _timeIndex = 0;
                        }
                        var total = 0L;
                        for (int i = 0; i < BUFF_SIZE; i++)
                        {
                            total += _timeBuf[i];
                        }
                        timeDiffMs = total / BUFF_SIZE;
                    }
                    else
                    {
                        timeInited = true;
                        var time = value;
                        for (int i = 0; i < BUFF_SIZE; i++)
                        {
                            _timeBuf[i] = time;
                        }
                        timeDiffMs = time;
                    }
#if SERVER
                }
#endif
            }
        }
    }
}
