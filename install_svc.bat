@ECHO OFF

ECHO To use this script, you need to drop ispyservice.exe into %WINDIR%. Continue?
PAUSE
sc create ispy binPath= %windir%\ispyservice.exe start= auto
ECHO !!! You now need to change LOG ON type of the service "ispy" to your account !!!
