# P2Loader
 Propeller P2 code Loader in C#

It will load a program by com port or WiFi port.

P2Loader com10 program.binary

P2Loader wx-de345c program.binary

P2Loader 192.168.4.1 program.binary

It will also load ELF files

P2Loader com10 program.elf

It also supports patching the frequency and baud rate values

P2Loader patch 200 230400 com10 program.elf

The patch values are 100 - 200 Mhz and baud rate
Wifi only supports baud rates up to 921600.
