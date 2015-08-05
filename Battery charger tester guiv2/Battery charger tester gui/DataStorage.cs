using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Battery_charger_tester_gui
{
    class DataStorage
    {

        private static DataStorage dataStorage;

        private Boolean verbosity = true; // set this to change the amount of data going to the serial monitor window, RichTextBox1
        // variables for storing current and past values
        /**********************************    Define number of ADC channels to measure from     **************************/
        private int numADCChannels;
        /******************************************************************************************************************/
        private UInt16[] ADCCounts;
        private decimal[] decimalValues;
        private decimal[] scalingFactors = {
                                       /* Channel 1 scaling factor */ 6.06000M,  // 6.06 multiplier for 3.3V to 20V
                                       /* Channel 2 scaling factor */ 1.51515M,   // 1.5151 multiplier for 3.3V to 5V
                                       /* Channel 3 scaling factor */ 1.51515M,  // 1.5151 multiplier for 3.3V to 5V
                                       /* Channel 4 scaling factor */ 1.51515M,//1.5151515151515151M; // to scale the 3.3V count to 5V. Voltage dividers must be used.,
                                       /* Channel 5 scaling factor */ 1.00000M, // same scale as the battery voltage scaling factor.
                                       /* Channel 6 scaling factor */ 1.00000M, // conversion factor for voltage count to current. Must be CALIBRATED
                                       /* Channel 7 scaling factor */ 1.00000M,// 2.727272
                                       /* Channel 8 scaling factor */ 1.00000M,
                                       /* Channel 9 scaling factor */ 1.00000M,
                                       /* Channel 10 scaling factor */ 1.00000M,
                                       /* Channel 11 scaling factor */ 1.00000M,
                                       /* Channel 12 scaling factor */ 1.00000M};
        private string[] units = {
                               /* Channel 1 units (V or A usually) */ "V",
                               /* Channel 2 units (V or A usually) */ "V",
                               /* Channel 3 units (V or A usually) */ "V",
                               /* Channel 4 units (V or A usually) */ "V",
                               /* Channel 5 units (V or A usually) */ "A",
                               /* Channel 6 units (V or A usually) */ "A",
                               /* Channel 7 units (V or A usually) */ "A",
                               /* Channel 8 units (V or A usually) */ "A",
                               /* Channel 9 units (V or A usually) */ "V",
                               /* Channel 10 units (V or A usually) */ "V",
                               /* Channel 11 units (V or A usually) */ "V",
                               /* Channel 12 units (V or A usually) */ "V"
                         };
        // logging rates to choose from in the drop-down
        public readonly int[] logrates = {200, 500, 900, 1000, 2000, 5000, 10000, 20000, 30000, 60000, 120000, 300000 };
        // Array of duty cycle choices for the load circuit timing
        public readonly string[] dutyCycleChoices = { "off(no current draw)", "10-10-80 (6,6,48)", "5-5-90 (3,3,54)", 
                                  "5-45-50 (3,27,30)", "100 mA","250 mA",
                                  "500 mA", "750 mA", "1000 mA", "1500 mA", "2000mA", "2500mA", "3000mA"};
        private int currentDutyCycle; // default is 0x00 for 5-5-90
        private const decimal voltsPerCount = (decimal)(3.3 / 65535); // volts per count will be the max volts divided by the max counts

        private DataStorage()
        {
            // constructor is empty, all values are consts or declared later when size is known.
            currentDutyCycle = 0; // initialize duty cycle to zero
        }
        // gets instance or creates one if there is none
        public static DataStorage getInstance()
        {
            if (dataStorage == null)
            {
                dataStorage = new DataStorage();
            }
            return dataStorage;
        }

        // method to set verbosity of the GUI displays
        public void setVerbosity(Boolean verbosity)
        {
            this.verbosity = verbosity;
        }

        // method to read the verbosity of the displays
        public Boolean getVerbosity()
        {
            return this.verbosity;
        }

        // set the numADCChannels
        public void setNumADCChannels(int numADCChannels)
        {
            this.numADCChannels = numADCChannels;
            this.ADCCounts = new UInt16[numADCChannels];
            this.decimalValues = new decimal[numADCChannels];
        }

        // return the number of ADC channels
        public int getNumADCChannels()
        {
            return this.numADCChannels;
        }

        // method to check if duty cycle input is valid
        public void checkDutyCycle(int dutyCycle)
        {
            if (dutyCycleChoices.Length < dutyCycle)
            {
                throw new Exception("invalid duty cycle setting.");
            }
        }
        // method to set current duty cycle
        public void setCurrentDutyCycle(int currentDutyCycle)
        {
            checkDutyCycle(currentDutyCycle);
            this.currentDutyCycle = currentDutyCycle;
        }

        // method to get current dutyCycle
        public int getCurrentDutyCycle()
        {
            return this.currentDutyCycle;
        }

        // method to set ADCCount
        public void setADCCount(int channel, UInt16 count)
        {
            ADCCounts[channel] = count;
            decimal countToDec = count;
            decimalValues[channel] = Decimal.Round(countToDec * voltsPerCount * scalingFactors[channel], 5);
        }

        // method to get ADC counts
        public UInt16 getADCCount(int channel)
        {
            return ADCCounts[channel];
        }

        // method to get decimal value for ADC channel input
        public decimal getDecimalValues(int channel)
        {
            return decimalValues[channel];
        }

        // method to get log rates
        public int[] getLogRates()
        {
            return logrates;
        }

        public string getUnits(int channel)
        {
            return this.units[channel];
        }
    }
}
