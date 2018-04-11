# WFNodeServer

#### Installation

Run the WFNodeServer.exe binary.  If this is the first time this has been run,
it will prompt you to go to the configuration setup page using a web browser.

Enter the IP address of the machine running the WFNodeServer.exe binary and
the indicated port number in a browser window. For example:

    http://192.168.0.45:8288/config

The browser should show the WFNodeServer's configuation page.  Here you must
enter the IP Address (and port if it's not 80) of your ISY along with the
ISY username and password.  Once those have been entered, click on any of
the "Set" buttons to save the information.

The program should now connect to the ISY, attempt to install the node
server configuration on the ISY and start listening for data on the 
WeatherFlow UDP port of 50222. 

To properly calculate the barometric sealevel pressure, the program needs
to know the elevation of the Air sensor.  To set this up, enter your
WeatherFlow station identification number and click on the "Add" button.

The station information, including the elevation should now be populated.

If you want to see rapid wind data, make sure the rapid checkbox is checked
for your station.

If you want to see additional sensor device data, check the device data
checkbox.

#### Requirements

1) A windows computer running Windows 7 or later OR a linux computer with
   MONO installed.
 
2) A WeatherFlow smart weather station

3) A Unviersal Devices ISY994 controller with firmware 5.0.12 or later.

