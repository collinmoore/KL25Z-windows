# KL25Z-windows
code for interfacing the KL25z MCU and Windows using C on the MCU and C# in Windows.

C# program talks to the KL25z over the SDA port via a serialPort
  C# solution file is in the folder "Battery charger tester guiv2"
edit the dataStorage.cs class with the multipliers and units for all the ADC channels you want.

I plan to fork this into one version for logging, and one version for on-demand reading of the ADC.
