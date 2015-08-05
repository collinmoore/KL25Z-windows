using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace Battery_charger_tester_gui
{
    public partial class Form1 : Form
    {
        private LabelManager labelManager;
        private DataLogger dataLogger;
        private ConnectionManager connectionManager;
        private DataStorage dataStorage;
        private static Form1 form1;
        delegate void SetTextCallback(Label label, String text);
        delegate void displayAndButtonDelegate();
        delegate void textboxDelegate(String input);
        ThreadManager threadManager;
        Boolean logstartClicked = false;


        // sound player, for when battery charging is done
        System.Media.SoundPlayer player = new System.Media.SoundPlayer(@"C:\WINDOWS\Media\notify.wav");
        private Form1()
        {
            InitializeComponent();
            Label[] labels = { label1, label2, label3, label4, label5, label6, label7, label8, label9, label10, label11, label12 };
            ArrayList labelList = new ArrayList(labels);
            this.labelManager = new LabelManager(labelList);
            this.dataLogger = DataLogger.getInstance(this);
            this.connectionManager = ConnectionManager.getInstance(this);
            this.dataStorage = DataStorage.getInstance();

            // logging rate selection
            foreach (int i in dataStorage.getLogRates())
            {
                comboBox3.Items.Add(i);
            }
            startLogging.Enabled = false;

            /* populate duty cycle drop-down liwt */
            foreach (string i in dataStorage.dutyCycleChoices)
            {
                dutyCycleDropDown.Items.Add(i);
            }

            // button9 and button10 control the serial data transfer timeout enable
            // system starts with serialTiemoutEnable true, and the option to disable it on
            enableTimeout.Enabled = false; // button10 is enable
            disableTimeout.Enabled = true; // button9 is disable
            enableTimeout.Text = "";
            refreshData.Enabled = false;
            startLogging.Enabled = false;
            // richTextBox1 is where the serial output is displayed.

            // disable the logging buttons until the files exist
            wipeLogs.Enabled = false;
            startLogging.Enabled = false;
            //   comboBox3.SelectedIndex = 3;

        }

        // method to create singleton instance of Form1
        public static Form1 getInstance()
        {
            if (form1 == null)
            {
                form1 = new Form1();
            }
            return form1;
        }

        // method to invoke to  update labels and log files, and update color of logging button
        public void dataAndButton()
        {
            if (InvokeRequired)
            {
                form1.BeginInvoke(new displayAndButtonDelegate(displaysAndButtonUpdate));
            }
            else displaysAndButtonUpdate();
        }
        private void displaysAndButtonUpdate()
        {
            if (startLogging.BackColor == Color.DarkGreen)
            {
                startLogging.BackColor = Color.LightGreen;
            }
            else
            {
                startLogging.BackColor = Color.DarkGreen;
            }
            Boolean updated = false;
            updated = refreshAllData();
            if (updated)
            {

            }
            else
            {
                /* tell syslog that data was NOT updated properly */
                try
                {
                    dataLogger.writeToLogfile(0, "Failed to update logs at " + DateTime.Now.ToString("h:mm:ss tt") + "\r");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                richTextBox1.AppendText("Failed to update logs at " + DateTime.Now.ToString("h:mm:ss tt") + "\r");
            }
        }

        // updates the text in richTextBox1 with a string input
        public void appendToRichTextBox1(string input)
        {
            if (InvokeRequired)
            {
                richTextBox1.BeginInvoke(new textboxDelegate(appendToRichTextBox1), new object[] { input });
            }
            else
            {
                richTextBox1.AppendText(input);
            }
        }

        // sets the text of a label
        private void setLabel(Label label, string text)
        {
            if (label.InvokeRequired)
            {
                SetTextCallback rx = new SetTextCallback(setLabel);
                Invoke(rx, new object[] { label, text });
            }
            else
            {
                label.Text = text;
            }
        }

        // combobox 3 is the log rate selection
        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            startLogging.Enabled = true;
            startLogging.Text = "Start logging";
            label14.Text = Convert.ToString(dataStorage.logrates[comboBox3.SelectedIndex]);
            dataLogger.setLogRate(dataStorage.logrates[comboBox3.SelectedIndex]);

        }

        // button 1 opens the port
        private void button1_Click(object sender, EventArgs e) // button1 is the button to open the serial port
        {
            connectButton.Enabled = false;
            if ((connectButton.Text == "Connect") | (connectButton.Text == "Retry")) // on clicking open port, the following should happen
            {
                connectButton.Enabled = false;
                startLogging.Enabled = false;
                wipeLogs.Enabled = false;
                refreshData.Enabled = false;
                Boolean connected = connectionManager.connect();

                connectButton.Enabled = true;
                if (connected)
                {
                    connectButton.Text = "Disconnect";
                    connectButton.BackColor = Color.LightGreen;
                    refreshData.Enabled = true;
                    dataStorage.setNumADCChannels(dataStorage.getNumADCChannels());
                    startLogging.Enabled = true;
                    wipeLogs.Enabled = true;
                }
                else
                {
                    connectButton.Text = "Retry";
                    connectButton.BackColor = Color.DarkRed;
                    appendToRichTextBox1("Error: did not receive handshake from MCU\r Not connected.\r");
                    startLogging.Enabled = false;
                    refreshData.Enabled = false;

                }
            }
            else if (connectButton.Text == "Reconnect")
            { // reconnect just opens the port that already worked.
                connectionManager.reconnect();
                connectButton.Text = "Disconnect";
                connectButton.BackColor = Color.LightGreen;
                startLogging.Enabled = true;
                refreshData.Enabled = true;
            }
            else if (connectButton.Text == "Disconnect")
            { // if button one says close port, disconnect
                connectionManager.disconnect();
                connectButton.Text = "Reconnect";
                connectButton.BackColor = Color.DarkOrange;
                startLogging.Enabled = false;
                refreshData.Enabled = false;
            }
            connectButton.Enabled = true;
        }

        // button2 clears the terminal window
        private void button2_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
        }

        // start or stop the logger
        private void button6_Click(object sender, EventArgs e)
        {
            if (!logstartClicked)
            {
                dataLogger.prepareLogFiles(dataStorage.getNumADCChannels());
                logstartClicked = true;
            }
            if (startLogging.Text == "Start logging" | startLogging.Text == "Start append")
            {
                dataLogger.startLogTimer(); // start the log timer, with default rate if not selected.
                startLogging.Enabled = true;
                startLogging.Text = "Stop logging";
                startLogging.BackColor = Color.DarkGreen;
                /* log start of logging in redundant redundancy system log */
                dataLogger.writeToLogfile(0, "Started logging data at "
                    + DateTime.Now.ToString("h:mm:ss tt") + " every " + dataStorage.logrates[comboBox3.SelectedIndex] + " ms.\r");
            }
            else if ((startLogging.Text == "Stop logging") | (startLogging.Text == "Error, WAT?"))
            {
                startLogging.BackColor = Color.DarkOrange;
                dataLogger.stopLogTimer();
                startLogging.Text = "Start append";
                /* log start append in system log */
                dataLogger.writeToLogfile(0, "Stopped logging data at "
                      + DateTime.Now.ToString("h:mm:ss tt") + " *****.\r");
            }
        }

        // button 7 clears the log data
        private void button7_Click(object sender, EventArgs e)
        {
            try
            {
                dataLogger.clearLogs();
            }
            catch (System.NullReferenceException ex)
            {
                dataLogger.prepareLogFiles(dataStorage.getNumADCChannels());
                dataLogger.clearLogs();
            }
            dataLogger.writeToLogfile(0, "Wiped logs at " + DateTime.Now.ToString("h:mm:ss tt") + "\r");
        }

        // button10 is enable for serial transfer timeout
        private void button10_Click(object sender, EventArgs e)
        {
            enableTimeout.Enabled = false;
            enableTimeout.Text = "";
            disableTimeout.Enabled = true;
            disableTimeout.Text = "Disable";
            groupBox22.BackColor = Color.DarkGreen;
            connectionManager.enableSerialTimeout();
        }

        // button9 is disable for serial timeout
        private void button9_Click(object sender, EventArgs e)
        {
            disableTimeout.Enabled = false;
            disableTimeout.Text = "";
            enableTimeout.Enabled = true;
            enableTimeout.Text = "Enable";
            groupBox22.BackColor = Color.DarkRed;
            connectionManager.disableSerialTimeout();
        }

        // Button12 is to refresh the ADC readings and duty cycle information
        private void button12_Click(object sender, EventArgs e)
        {
           // connectionManager.readADC(0);
            Boolean refreshed = refreshAllData();
            if (refreshed)
            {
                refreshData.Text = "Refresh Data";
            }
            else
            {
                refreshData.BackColor = Color.DarkRed;
                refreshData.Text = "Failed update";
            }
        }

        // comboBox7 is the duty cycle choice drop-down
        private void comboBox7_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                dutyCycleDropDown.Text = "Updating...";
                connectionManager.setDutyCycle((Byte)dutyCycleDropDown.SelectedIndex); // register is 0x11 for IEND, addressed as 0x0F by this code (to fit into registers[] array)
                // connectionManager.readDutyCycle();
                dutyCycleDropDown.Text = (dataStorage.dutyCycleChoices[dataStorage.getCurrentDutyCycle()]);
                richTextBox1.AppendText("Duty cycle set to " + dataStorage.getCurrentDutyCycle() + "\r");
                Boolean refreshed = refreshAllData();
            }
            catch (Exception ex)
            {
                dutyCycleDropDown.Text = "Failed";
                MessageBox.Show(ex.Message);
            }
        }

        private Boolean refreshAllData()
        {
            Boolean freshADC = refreshADCData();
            Boolean freshDutyCycle = readDutyCycle();
            if (freshADC & freshDutyCycle)
            {
                Boolean displayUpdated = updateLabels();
                if (displayUpdated & freshADC & freshDutyCycle)
                {
                    appendToRichTextBox1("Successfully refreshed.\r");
                    refreshData.BackColor = Color.DarkGreen;
                    return true;
                }
                else
                {
                    groupBox4.BackColor = Color.DarkRed;
                    label2.Text = "YES";
                    refreshData.BackColor = Color.DarkRed;
                    if (!freshADC)
                    {
                        appendToRichTextBox1("Failed to update ADC values.\r");
                        label3.Text = "ADC Error";
                    }
                    if (!freshDutyCycle)
                    {
                        appendToRichTextBox1("Failed to read duty cycle. \r");
                        label4.Text = "Duty Cycle Read Error";
                        groupBox23.BackColor = Color.DarkRed;
                    }
                    return false;
                }
            }
            else
            {
                appendToRichTextBox1("Failed to read ADC and duty cycle, are you connected?");
                return false;
            }
        }

        // read the setting of the duty cycle.
        private Boolean readDutyCycle()
        {
            try
            {
                connectionManager.readDutyCycle();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            } // labels get updated when the serial port has received stuff and changed stored values
        }

        // Calls refreshRegisterData and refreshADCData and then updates display with info
        private Boolean refreshADCData()
        {
            try
            {
                for (int i = 0; i < dataStorage.getNumADCChannels(); i++)
                {
                    connectionManager.readADC(i);
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        // update the text boxes with what is in the registers
        private Boolean updateLabels()   // returns a boolean if it updated successfully
        { // updates all displays on the GUI

            /********** now update the labels that display the measurements **************/
            try
            {
                for (int i = 0; i < dataStorage.getNumADCChannels(); i++)
                {
                    labelManager.setText("" + dataStorage.getDecimalValues(i) + " " + dataStorage.getUnits(i), i);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ADC values are null, connect to MCU first.");
                appendToRichTextBox1(ex.Message);
            }
            /* update background colors for boxes that should be within certain ranges */
            decimal inVoltage = dataStorage.getDecimalValues(0);
            decimal usbVoltage = dataStorage.getDecimalValues(1);
            decimal batteryVoltage = dataStorage.getDecimalValues(2);
            decimal systemVoltage = dataStorage.getDecimalValues(3);
            decimal inCurrent = dataStorage.getDecimalValues(4);
            decimal usbCurrent = dataStorage.getDecimalValues(5);
            decimal batteryCurrent = dataStorage.getDecimalValues(6);
            decimal sysCurrent = dataStorage.getDecimalValues(7);

            decimal inVoltageLowLimit = 4.20M;
            decimal inVoltageLimit = 18.00M;
            decimal inVoltageOperatingLimit = 10.00M;
            decimal usbVoltageLowLimit = 4.40M;
            decimal usbVoltageUpperLimit = 5.25M;
            decimal usbOverVoltageThreshold = 5.50M;
            decimal lowBattVoltage = 2.800M;
            //decimal batteryTerminationVoltage = 4.20M;
            decimal midLowBatteryVoltage = 3.20M;
            decimal upperSysVLIM = 4.30M;
            decimal lowerSysVLIM = 3.40M;
            decimal sysILIM = 3.00M;

            // battery voltage safe limits
            if (batteryVoltage > midLowBatteryVoltage)
            {
                groupBox25.BackColor = Color.DarkGreen;
            }
            else if ((batteryVoltage > lowBattVoltage) & (batteryVoltage < midLowBatteryVoltage))
            {
                groupBox25.BackColor = Color.DarkOrange;
            }
            else
            {
                groupBox25.BackColor = Color.DarkRed;
                dutyCycleDropDown.SelectedIndex = 0;
            }
            // System voltage indication
            if ((systemVoltage < upperSysVLIM) & (systemVoltage > lowerSysVLIM))
            {
                groupBox19.BackColor = Color.DarkGreen;
            }
            else
            {
                groupBox19.BackColor = Color.DarkRed;
            }
            // USB voltage should be between 4.4V and 5.25/5.5V
            if ((usbVoltage < usbVoltageUpperLimit) & (usbVoltage > usbVoltageLowLimit))
            {
                groupBox2.BackColor = Color.DarkGreen;
            }
            else if ((usbVoltage > usbVoltageUpperLimit) & (usbVoltage < usbOverVoltageThreshold))
            {
                groupBox2.BackColor = Color.DarkOrange;
            }
            else
            {
                groupBox2.BackColor = Color.DarkRed;
            }
            // Current measurement safe limits
            if (sysCurrent < sysILIM)
            {
                groupBox7.BackColor = Color.DarkGreen;
            }
            else
            {
                groupBox7.BackColor = Color.DarkRed;
            }
            // IN input safe limits
            if ((inVoltage < inVoltageLimit) & (inVoltage > inVoltageOperatingLimit))
            {
                groupBox26.BackColor = Color.DarkOrange;
            }
            else if ((inVoltage < inVoltageOperatingLimit) & (inVoltage > inVoltageLowLimit))
            {
                groupBox26.BackColor = Color.DarkGreen;
            }
            else
            {
                groupBox26.BackColor = Color.DarkRed;
            }
            /* update duty cycle display label */
            label13.Text = (dataStorage.dutyCycleChoices[dataStorage.getCurrentDutyCycle()]); // show current duty cycle
            // after doing everything, return true or false
            return true;
        }
    }
}
