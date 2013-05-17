echo "Verify that you have the follow before continue"
echo "Have cygwin with gstreamer installed in c:\cygwin"
pause

$curPath = Split-Path ($MyInvocation.MyCommand.Path)

if ($args[0] -eq "link")
{
    cmd /c mklink $env:windir\camrec.exe $curPath\bin\Debug\camrec.exe
}
else
{
    Copy-Item -Path $curPath\bin\Debug\camrec.exe -Confirm -Destination $env:windir\camrec.exe
}

Copy-Item -Path $curPath\camrec.sample.xml -Confirm -Destination $env:windir\camrec.xml

echo "Creating service"
cmd /c sc create camrec binPath= "%windir%\camrec.exe" start= auto

echo "****"
echo "Done. A sample config file camrec.xml is dropped along with camrec.exe."
echo "You need to modify the config file according to your situation."
