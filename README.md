# How to run

Everything except the mobile app is dockerized and can be started with a single command.

## Start the environment

Run:
```powershell
docker compose -f .\local_environment.dockercompose.yaml up
```

This will start:
- Database (PostgreSQL)
- Blob Storage (Azurite)
- API
- Frontend (SPA)
- Converter service
- Initializer (seeds data)

### Check Initialization
Check logs of the Initializer container to ensure data is ready:
`docker logs tbdanceInitializer`

When you see "Data initialized", the application is ready to use.

## Try to login

Once the environment is up, you can access the frontend at `http://localhost:3000`.

Test credentials:
- **Login:** testemail@email.com
- **Password:** 1234

## Mobile App

The mobile app is not included in the docker-compose environment. To run it, you need to open the solution in an IDE (like Rider or Visual Studio) and run the `TB.DanceDance.Mobile` project targeting your desired platform (Android, iOS, etc.).

Ensure the docker environment is running so the mobile app can connect to the API.
