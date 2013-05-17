@echo Verify that you have the follow before continue
@echo Have cygwin with gstreamer installed in c:\cygwin
@pause

copy /-Y %~dp0bin\Debug\camrec.exe %windir%
copy /-Y %~dp0camrec.sample.xml %windir%\camrec.xml
@echo creating service...
sc create camrec binPath= "%windir%\camrec.exe" start= auto

@echo Done. A sample config file camrec.xml is dropped along with camrec.exe.
@echo You need to modify the config file according to your situation.