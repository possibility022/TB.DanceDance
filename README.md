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

## Database tools

Download it and extract.
https://www.mongodb.com/try/download/shell
Direct Link: https://downloads.mongodb.com/compass/mongosh-1.7.1-win32-x64.zip

https://www.mongodb.com/try/download/database-tools
Direct Link: https://fastdl.mongodb.org/tools/db/mongodb-database-tools-windows-x86_64-100.6.1.zip


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



# Database copy

To make a database copy you will need a mongo database tools.

https://www.mongodb.com/try/download/database-tools

We will need to run mongodump and mongorestore.

Create a config file where URI to db will be stored. Do not add the file to git repository.

``` yaml
# /ignored/dbConfig.yaml

uri: mongodb+srv://username:password@cluster0.******.mongodb.net
```

To make a backup:

`mongodump.exe --config=dbConfig.yaml --archive=dancedancedb`

To Restore:

`mongorestore.exe --archive=dancedancedb`
