@echo off
echo Verify that you have the follow before continue
echo Dropped camrec.exe to %windir%
echo Have cygwin with gstreamer installed in c:\cygwin
pause

echo creating necessary directory
mkdir c:\camrec
echo creating service
sc create camrec binPath= "%windir%\camrec.exe" start= auto

echo Done. You need to create c:\camrec\config.txt with three lines:
echo * camcam URL (mjpeg)
echo * username
echo * password
