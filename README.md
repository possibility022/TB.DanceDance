# Local setup

## Database

Download mongodb Community Server

https://www.mongodb.com/try/download/community

Install or unzip.

Create your own script to start service. Example:


``` ps
# startDb.ps1
$mongoDbExe = 'D:\Programy\mongodb-win32-x86_64-windows-6.0.4\bin\mongod.exe'
$mongoArgs = @('--dbpath="d:\data\mongodb\dancedance"')

& $mongoDbExe $mongoArgs
```

## Video container

Install azurite

https://github.com/Azure/Azurite

`npm install -g azurite`

Start it
`azurite -s -l c:\azurite -d c:\azurite\debug.log`

You can add it to script

``` ps
# startDb.ps1

$mongoDbExe = 'D:\Programy\mongodb-win32-x86_64-windows-6.0.4\bin\mongod.exe'
$mongoArgs = @('--dbpath="D:\data\dancedance\mongodb"')

Start-Process $mongoDbExe -ArgumentList $mongoArgs

# npm install -g azurite
azurite -l d:\data\dancedance\azurite\
```

