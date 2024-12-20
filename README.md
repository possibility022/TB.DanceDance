# How to run

## Host database and blob storage with docker

A docker compose file is prepared to run database, storage and also prepare test data.
Run:
```
docker compose -f .\local_environment.dockercompose.yaml up
```
Check logs of Initializer container: `docker logs tbdanceInitializer`.
When you can see "Data initialized" you can start using application on your local host.
Initializer is seeding database and blob storage.

## Build and Run

Build and run API
```
cd .\TB.DanceDance.API
dotnet run
```

On 2nd terminal - build and run frontend
```
cd .\tb.dancedance.frontend
npm install
npm run start

```

# Try to login

Application should be ready to use.
Test credentials:
- Login: testemail@email.com
- Password: 1234
