$mongoDbExe = 'D:\Programy\mongodb-win32-x86_64-windows-6.0.4\bin\mongod.exe'
$mongoArgs = @('--dbpath="D:\data\dancedance\mongodb"')

Start-Process $mongoDbExe -ArgumentList $mongoArgs

# npm install -g azurite
azurite -l d:\data\dancedance\azurite\