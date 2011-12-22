using System;

using System.Threading;



namespace Service
{

    public delegate void TickDelegate();



    public class CustomTimer
    {

        #region Events

        /// <summary>


        /// The tick event, will be triggered on every tick.


        /// </summary>


        public event TickDelegate Tick;

        #endregion



        #region Fields

        private Timer m_Timer;

        #endregion



        #region Properties

        /// <summary>


        /// The period between the ticks


        /// </summary>


        public int Period { get; set; }



        /// <summary>


        /// The time to delay before the callback is invoked


        /// </summary>


        public int DueTime { get; set; }

        #endregion



        #region Construct

        public CustomTimer() : this(0, 0) { }

        public CustomTimer(int period) : this(period, 0) { }

        public CustomTimer(int period, int dueTime)
        {

            Period = period;

            DueTime = dueTime;

        }

        #endregion



        #region Methods

        protected void OnTick()
        {

            if (Tick != null)
            {

                Tick();

            }

        }



        /// <summary>


        /// Start the timer


        /// </summary>


        public void Start()
        {

            TimerCallback timerCallback = new TimerCallback(ProcessTimerCallback);

            m_Timer = new Timer(

                timerCallback,

                null,

                DueTime,

                Period

            );



        }



        /// <summary>


        /// Stop the timer


        /// </summary>


        public void Stop()
        {

            if (m_Timer != null)
            {

                m_Timer.Dispose();

                m_Timer = null;

            }

        }



        private void ProcessTimerCallback(object o)
        {

            OnTick();

        }

        #endregion

    }

}
