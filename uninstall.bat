@echo stopping service camrec...
net stop camrec
sc delete camrec
del %windir%\camrec.exe

@echo !! You may want to manually delete %windir%\camrec.xml !!
