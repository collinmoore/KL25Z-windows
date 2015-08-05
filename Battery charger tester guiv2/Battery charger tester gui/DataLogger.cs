using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Battery_charger_tester_gui
{
    class DataLogger
    {
        private static DataLogger instance;
        private ConnectionManager connectionManager;
        private string[] logfiles;
        private System.Timers.Timer logTimer;
        private Form1 form1;
        private DataStorage dataStorage;
        private int lograte; // default log rate tick timer in ms
        private long elapsedMillis;
        // Constructor for DataLogger
        private DataLogger()
        {
            this.dataStorage = DataStorage.getInstance();
            this.lograte = 1000; // default log rate
            this.elapsedMillis = 0;
            logTimer = new System.Timers.Timer();
        }

        // makes a new instance of instance, given a pointer to Form1
        private void setForm(Form1 form1)
        {
            this.form1 = form1;
        }

        private void setConnManager(ConnectionManager conn1)
        {
            this.connectionManager = conn1;
        }

        public static DataLogger getInstance(Form1 form1)
        {
            instance = getInstance();
            instance.setForm(form1);
            return instance;
        }

        public static DataLogger getInstance(ConnectionManager conn1)
        {
            instance = getInstance();
            instance.setConnManager(conn1);
            return instance;
        }

        // getInstance method for singleton instance
        public static DataLogger getInstance()
        {
            if (instance == null)
            {
                instance = new DataLogger();
            }
            return instance;
        }

        public bool prepareLogFiles(int numChannels)
        {
            logfiles = new string[dataStorage.getNumADCChannels() + 2];
            logfiles[0] = "Systemlog.txt"; // system log has overview of when logging started/stopped
            for (int i = 1; i <= dataStorage.getNumADCChannels(); i++)
            {
                //logfile names for each channel measured
                logfiles[i] = "Channel" + i + "_log.csv";
            }
            
            logfiles[dataStorage.getNumADCChannels()+1] = "DutyCycleLog.csv"; // log of duty cycle setting.
            // start logs for all variables, delete the old ones first.
            // delete the old one if there is one.
            foreach (string n in logfiles)
            {
                if (System.IO.File.Exists(n))
                {
                    System.IO.File.Delete(n); // delete the old one if there is one.
                }
                System.IO.File.AppendAllText(n, "************** begin log file at "
                    + DateTime.Now.ToString("h:mm:ss tt") + " **************\r", Encoding.UTF8);
            }

            return true;
        }

        // clear measurement logs
        public void clearLogs()
        {
            for (int i = 1; i <= dataStorage.getNumADCChannels(); i++)
            {
                if (System.IO.File.Exists(logfiles[i]))
                {
                    System.IO.File.Delete(logfiles[i]); // delete the old one if there is one.
                }
                writeToLogfile(i, "***** begin channel " + i + " log file at "
                    + DateTime.Now.ToString("h:mm:ss tt") + " ****  ,");
            }
            writeToLogfile(0, " ****************** Cleared data logs at "
                    + DateTime.Now.ToString("h:mm:ss tt") + " **************\r");
            writeToLogfile((dataStorage.getNumADCChannels() + 1), "*****  begin duty cycle log file at "
                    + DateTime.Now.ToString("h:mm:ss tt") + " ****  ,");
        }

        // set the logging rate
        public void setLogRate(int tickRate)
        {
            this.lograte = tickRate;
            logTimer.Interval = tickRate;
        }

        // start the logging timer
        public void startLogTimer()
        {
            /*      TIMER FOR LOGGING       */
            logTimer.Elapsed += new ElapsedEventHandler(logTimer_Tick);
            logTimer.Interval = this.lograte; // timer interval in milliseconds.
            logTimer.Start();
        }

        // stop log timer
        public void stopLogTimer()
        {
            logTimer.Stop();
        }

        // timer tick event handler
        private void logTimer_Tick(object sender, EventArgs e)
        {
            elapsedMillis += (long)logTimer.Interval;
            if (dataStorage.getVerbosity())
            {
                form1.appendToRichTextBox1("Timer tick, refershing logs\r");
            }
            form1.dataAndButton(); // triggers update of all variables
            logADCData();
            logDutyCycle();
        }

        public void logADCData()
        {
            /* log each channel's value in decimal form. */
            for (int i = 1; i <= dataStorage.getNumADCChannels(); i++)
            {
                System.IO.File.AppendAllText(logfiles[i], dataStorage.getDecimalValues(i-1) + "," + elapsedMillis + "\r", Encoding.UTF8);
            }
            /* update system log file */
            System.IO.File.AppendAllText(logfiles[0], "Logged all channel values at " + DateTime.Now.ToString("h:mm:ss tt") + "\r", Encoding.UTF8);
            if (dataStorage.getVerbosity())
            {
                form1.appendToRichTextBox1("Logged all ADC channel data at "+DateTime.Now.ToString("h:mm:ss tt")+"\r");
            }
        }

        // method to log state of duty cycle
        public void logDutyCycle()
        {
            /* update duty cycle log file */
            System.IO.File.AppendAllText(logfiles[dataStorage.getNumADCChannels() + 1], dataStorage.getCurrentDutyCycle() + "," + elapsedMillis + "\r", Encoding.UTF8);
        }

        // method to write to the specified logfile
        public void writeToLogfile(int index, string text)
        {
            if (index < logfiles.Length)
            {
                System.IO.File.AppendAllText(logfiles[index], text, Encoding.UTF8);
            }
            else
            {
                throw new Exception("logfile " + index + " does not exist.");
            }
        }
    }
}
