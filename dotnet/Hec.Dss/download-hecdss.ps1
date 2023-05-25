# may need the following line 
# Set-ExecutionPolicy Unrestricted -Scope CurrentUser

# downloads linux and windows native DSS DLL.

if( Test-Path -Path native-lib\win\hecdss.dll ){
 # file allready exists
}
else{
$baseURL = "https://www.hec.usace.army.mil/nexus/repository/maven-public/mil/army/usace/hec/hecdss/"
$version = "7-IQ-12"

$linux = $baseURL + $version+ "-linux-x86_64/hecdss-" + $version +"-linux-x86_64.zip"
$win = $baseURL + $version+ "-win-x86_64/hecdss-" + $version +"-win-x86_64.zip"

Remove-Item -Recurse -Force native-lib\linux
New-Item -Type Directory -Path native-lib\linux
cd native-lib\linux
Invoke-WebRequest -URI $linux -OutFile libhecdss.zip
cmd /r 7z e libhecdss.zip


Remove-Item -Recurse -Force native-lib\win
New-Item -Type Directory -Path native-lib\win
cd ..\win
Invoke-WebRequest -URI $win -OutFile hecdss.zip
cmd /r 7z e hecdss.zip
cd ..\..
}
cmd /r dir /s /b .\native-lib\