# Backfill Blob Content-Type

One-off maintenance script for DD-63. Sets `Content-Type` on video and thumbnail blobs
that predate the converter daemon setting it on upload (`videos`/`videostoconvert` ->
`video/webm`, `thumbnails` -> `image/jpeg`). Idempotent — blobs that already have the
correct type are skipped, so it's safe to re-run.

## Run

```bash
npm install
npx tsx backfill.ts "<blob-connection-string>"
```

Or set `AZURE_STORAGE_CONNECTION_STRING` instead of passing it as an argument.

Local Azurite connection string (matches `local_environment.dockercompose.yaml`):

```
AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;DefaultEndpointsProtocol=http;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;
```
