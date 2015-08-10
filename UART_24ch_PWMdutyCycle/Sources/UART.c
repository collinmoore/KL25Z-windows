/* UART.c
 *
 *  Created on: May 13, 2015
 *      Author: Beartooth
 */


#include "UART.h"
#include "RxBuf.h"
#include "AS1.h"
#include "Events.h"
#include "data.h"


/*****************************************************************************************
*   COMMANDS FOR UART INTERFACE
*   From PC to this chip
* 			
*******   0x00 are commands for reading ADC values  *******
*   	
* 	0x00  read from ADC channel X, from 0 to F (16)
* 		followed by channel number
* 			followed by UNUSED
*
*******  0x1X are commands NOT USED  *******
* 	
* 	0x10 write to register
* 		followed by register address (0x00-0x0F, 16 registers)
* 			followed by value to write
* 			
******  0x2X are NOT USED  ******
*
*
*
*******  0x30 is read duty cycle of constant load circuit  *******
* 	
* 	0x30 read duty cycle
* 		followed by UNUSED
* 			followed by UNUSED
* 	
****** 0x40 is the set duty cycle command  ******
* 	
* 	0x40 set duty cycle
* 		followed by cycle type:
* 		0x00 for OFF (no loads on ever)
* 		0x01 for 10-10-80 (6 seconds tx, 6 seconds rx, 48 seconds standby)
* 		0x02 for 5-5-90 (3 seconds tx, 3 seconds rx, 54 seconds standby)
* 		0x03 for 5-45-50 (3 seconds tx, 27 seconds rx, 30 seconds standby)
* 		0x04 for 100mA continuous load
* 		0x05 for 250mA continuous load
* 		0x06 for 500mA continuous load
* 		0x07 for 750mA continuous load
* 		0x08 for 1000mA continuous load
* 		0x09 for 1500mA continuous load
* 		0x0A for 2000mA continuous load
* 		0x0B for 2500mA continuous load
* 		0x0C for 3000mA continuous load
* 			followed by UNUSED
*
******  0x44 is handshake command
*		
*		0x44
*			followed by 0x13
*				followed by 0x37 
* 			
*************************************************************************************************************** 			
* 	Sent by this chip to PC:
* 	
*******  0x0X: Read ADC channel X command  *******
* 			followed by MSB of ADC count 
* 				followed by LSB of ADC count
* 	
*******  0x10: NOT USED  *******
* 			
* 			
* 			
*******  0x2X: Failed ADC read  ******
*			followed by F0 if failed to read ADC
* 				followed by F0 if failed to read ADC
* 			
*******  0x3X: read load circuit duty cycle for PWM channel X  *******
*			followed by duty cycle
*				followed by NOT USED
* 			
* 				
*******  0x4X: set load circuit duty cycle command  *******		
*			followed by old duty cycle setting
*				followed by new duty cycle setting
*				
******  0x99: response to handshake  ******
*			followed by number of ADC channels
*				followed by number of PWM channels
*	 
*******  0xAA: received unrecognized command  *******
* 			followed by command received
* 				followed by address received	
* 				
*******  0xFF means end of file  *******
*			followed by 0xFF
*				followed by 0xFF
*				
**********************************************************************************************************************
*/
/* possible received codes, instructions, then data1, then data2 */

const uint8 RX_INSTRUCTION_READ_ADC = 0x00U;
const uint8 RX_INSTRUCTION_READ_DUTY_CYCLE = 0x30U;
const uint8 RX_INSTRUCTION_SET_DUTY_CYCLE = 0x40U;
const uint8 RX_INSTRUCTION_HANDSHAKE = 0x99U;
const uint8 RX_DATA1_HANDSHAKE = 0x13U;
const uint8 RX_DATA2_HANDSHAKE = 0x37U;

/* possible transmit codes, instructions, then data1, then data2 */

/* ADC channel read instruction responses are dependent on which channel they are from, from 0x00 to 0x17 */

const uint8 TX_INSTRUCTION_READ_DUTY_CYCLE = 0X30U;
        
const uint8 TX_INSTRUCTION_SET_DUTY_CYCLE = 0X40U;

const uint8 TX_INSTRUCTION_HANDSHAKE = 0x99U;

const uint8 TX_INSTRUCTION_FAILED_ADC_READ = 0x20U;

const uint8 TX_INSTRUCTION_UNKNOWN_COMMAND = 0xAAU;

const uint8 TX_INSTRUCTION_EOF = 0xFFU;

const uint8 TX_DATA1_FAILED_ADC_READ = 0xF0U;
const uint8 TX_DATA1_INCORRECT_DUTY_CYCLE_SETTING = 0xF4U;
const uint8 TX_DATA1_EOF = 0xFFU;

const uint8 TX_DATA2_FAILED_ADC_READ = 0xF0U;
const uint8 TX_DATA2_HANDSHAKE = 0x37U;
const uint8 TX_DATA2_EOF = 0xFFU;

/*********************************************************************************************/

static UART_Desc deviceData;

bool haveReceivedPacket=FALSE;

void UART_Init(void) {
  /* initialize struct fields */
  deviceData.handle = AS1_Init(&deviceData);
  deviceData.isSent = FALSE;
  deviceData.rxChar = '\0';
  deviceData.rxPutFct = RxBuf_Put;
  /* set up to receive RX into input buffer */
  RxBuf_Init(); /* initialize RX buffer */
  /* Set up ReceiveBlock() with a single byte buffer. We will be called in OnBlockReceived() event. */
  while(AS1_ReceiveBlock(deviceData.handle, (LDD_TData *)&deviceData.rxChar, sizeof(deviceData.rxChar))!=ERR_OK) {} /* initial kick off for receiving data */
}

 void UART_GetPacket(byte *packet){
	uint8_t rx_counter=0;
	for(rx_counter=0;rx_counter<3;rx_counter++){
		 (void)RxBuf_Get(&packet[rx_counter]); // get the character in the buffer, store into address of the appropriate  
	}
	haveReceivedPacket=FALSE; // set the packet received back to 0
 }
 
 void UART_SendPacket(byte *tx_packet){
	uint8_t tx_counter=0; 
	for(tx_counter=0;tx_counter<3;tx_counter++){
		 deviceData.isSent = FALSE;  /* this will be set to 1 once the block has been sent */
		 while(AS1_SendBlock(deviceData.handle, (LDD_TData*)&tx_packet[tx_counter], 1)!=ERR_OK) {} /* Send the right byte */
		 while(!deviceData.isSent) {} /* wait until we get the green flag from the TX interrupt */
	}
 }

 void UART_ParseData(void){
	 /* values to be used by UART send/receive */
	 byte rx_packet[3] = {0};
	 byte tx_packet[3] = {0};
	 
	UART_GetPacket(rx_packet); // GetPacket will put the three received bytes into the packet
	if(rx_packet[0]==RX_INSTRUCTION_READ_ADC){ // based on command, the first byte
		if(adcReadStatus ==ERR_OK){
			tx_packet[0] = (rx_packet[0]|rx_packet[1]); // send back command and the channel ORed
			uint8_t offset = rx_packet[1]; // get the offset in words to start reading at, which is the ADC to read from.
			uint8 * halfword_ptr = (byte *)&voltageValues[offset]; // pointer to point to bytes instead of words, starting at word offset
			//tx_packet[2] = (uint8)(voltageValues[offset]&&0x00FF);
			//tx_packet[1] = (uint8)(voltageValues[offset]>>8);
			tx_packet[2] = *halfword_ptr; // byte-little-endian, LSB half sent first, must be read in and shifted into MSB half by PC
			tx_packet[1] = *(++halfword_ptr); // LSB is tx_packet[2], MSB is tx_packet[1]
		}
		else{
			tx_packet[0] = TX_INSTRUCTION_FAILED_ADC_READ; // failed to read ADC channel, just say 0
			tx_packet[1] = TX_DATA1_FAILED_ADC_READ;
			tx_packet[2] = TX_DATA2_FAILED_ADC_READ;
		}
	}
	else if(rx_packet[0]==RX_INSTRUCTION_READ_DUTY_CYCLE){
		/* read duty cycle on MCU */
		tx_packet[0] = TX_INSTRUCTION_READ_DUTY_CYCLE;
		tx_packet[1] = dutyCycle;
		tx_packet[2] = 0x00U;
	}
	else if(rx_packet[0]==RX_INSTRUCTION_SET_DUTY_CYCLE){
		/* set duty cycle on MCU */
		if(rx_packet[1]>0x0CU){ // if the PC tries to set a mode that doesn't exist, send back error
			tx_packet[1] = TX_DATA1_INCORRECT_DUTY_CYCLE_SETTING; // error writing, byte 1
			tx_packet[2] = rx_packet[1]; // send back the invalid value for duty cycle
		}
		else{
			uint8 oldDutyCycle = dutyCycle;
			dutyCycleCounter=0;
			firstDutySet = TRUE;
			dutyCycle = rx_packet[1]; // set duty cycle to either 1, 2, 2, 4, 5, 6, or 7
			tx_packet[1] = oldDutyCycle; // send back the old duty cycle
			tx_packet[2] = dutyCycle; // send back the new duty cycle
		}
		tx_packet[0] = TX_INSTRUCTION_SET_DUTY_CYCLE;
	}
	else if((rx_packet[0]==RX_INSTRUCTION_HANDSHAKE)
			&&(rx_packet[1]==RX_DATA1_HANDSHAKE)
			&&(rx_packet[2]==RX_DATA2_HANDSHAKE)){
		tx_packet[0] = TX_INSTRUCTION_HANDSHAKE;
		tx_packet[1] = AD1_CHANNEL_COUNT; // send the number of ADC channels on the device
		tx_packet[2] = TX_DATA2_HANDSHAKE; 
	}
	else{
		tx_packet[0] = TX_INSTRUCTION_UNKNOWN_COMMAND; // send error code for received unknown command
		tx_packet[1] = rx_packet[0]; // set the command to send
		tx_packet[2] = rx_packet[1]; // set the address to send
	}
	UART_SendPacket(tx_packet); // send the packet
/*	tx_packet[0]=TX_INSTRUCTION_EOF; // set EOF character FF at end of any transmission
	tx_packet[1]=TX_DATA1_EOF; // unused
	tx_packet[2]=TX_DATA2_EOF; // unused 
	UART_SendPacket(tx_packet); // send EOF packet to tell PC that we are done sending data */
} /* end UART_ParseData */
