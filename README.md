# KL25Z-windows
code for interfacing the KL25z MCU and Windows using C on the MCU and C# in Windows.

C# program talks to the KL25z over the SDA port via a serialPort
  C# solution file is in the folder "Battery charger tester guiv2"
edit the dataStorage.cs class with the multipliers and units for all the ADC channels you want.

I plan to fork this into one version for logging, and one version for on-demand reading of the ADC.

The KL25Z is controlled by the CodeWarrior project in UART_24ch_PWMdutyCycle folder.

Import project into codewarrior, build, and run. The KL25z will wait for the PC to connect.
The KL25z will read the ADC every second, so if the PC asks for readings, they could be up to a second old.
