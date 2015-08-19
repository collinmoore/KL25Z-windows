# KL25Z-windows
code for interfacing the KL25z MCU and Windows using C on the MCU and C# in Windows.

C# program talks to the KL25z over the SDA port via a serialPort
  C# solution file is in the folder "Battery charger tester guiv2"
edit the dataStorage.cs class with the multipliers and units for all the ADC channels you want.

This fork differs from the other fork in that this fork's KL25z code contains duty cycle settings for 
draining batteries, and only measures 8 channels. The other fork measures 12 channels and does not have 
any PWM or timer to control a load circuit.

The KL25Z is controlled by the CodeWarrior project in UART_24ch_PWMdutyCycle folder.

Import project into codewarrior, build, and run. The KL25z will wait for the PC to connect.
The KL25z will read the ADC every second, so if the PC asks for readings, they could be up to a second old.
