using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Battery_charger_tester_gui
{
    class ThreadManager
    {
        private ThreadListener listener;
        private Thread thread;
        private volatile String message;

        public ThreadManager(ThreadListener listener)
        {
            this.listener = listener;
        }



    }
}
