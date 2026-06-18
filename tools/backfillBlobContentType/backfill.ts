// One-off maintenance script for DD-63: sets the correct Content-Type on video and
// thumbnail blobs that predate the converter daemon setting it on upload. Safe to
// re-run — blobs that already have the correct Content-Type are left untouched.
import { BlobServiceClient } from "@azure/storage-blob";

const VIDEO_CONTAINERS = ["videos", "videostoconvert"];
const VIDEO_CONTENT_TYPE = "video/webm";

const THUMBNAIL_CONTAINER = "thumbnails";
const THUMBNAIL_CONTENT_TYPE = "image/jpeg";

const MISSING_CONTENT_TYPES = new Set(["", "application/octet-stream"]);

interface Summary {
  scanned: number;
  updated: number;
  skipped: number;
}

async function backfillContainer(
  blobServiceClient: BlobServiceClient,
  containerName: string,
  correctContentType: string,
): Promise<Summary> {
  const summary: Summary = { scanned: 0, updated: 0, skipped: 0 };
  const containerClient = blobServiceClient.getContainerClient(containerName);

  if (!(await containerClient.exists())) {
    console.log(`Container "${containerName}" does not exist, skipping.`);
    return summary;
  }

  for await (const blob of containerClient.listBlobsFlat()) {
    summary.scanned++;
    const currentContentType = blob.properties.contentType ?? "";

    if (currentContentType === correctContentType) {
      summary.skipped++;
      continue;
    }
    if (!MISSING_CONTENT_TYPES.has(currentContentType)) {
      // Has a real, different content type — leave it alone rather than overwrite.
      summary.skipped++;
      continue;
    }

    const blockBlobClient = containerClient.getBlockBlobClient(blob.name);
    await blockBlobClient.setHTTPHeaders({
      ...blob.properties,
      blobContentType: correctContentType,
    });
    summary.updated++;
    console.log(`Updated ${containerName}/${blob.name}: "${currentContentType}" -> "${correctContentType}"`);
  }

  return summary;
}

async function main() {
  const connectionString = process.argv[2] ?? process.env.AZURE_STORAGE_CONNECTION_STRING;

  if (!connectionString) {
    console.error(
      "Usage: npx tsx backfill.ts \"<connection-string>\"  (or set AZURE_STORAGE_CONNECTION_STRING)",
    );
    process.exit(1);
  }

  const blobServiceClient = BlobServiceClient.fromConnectionString(connectionString);

  const totals: Record<string, Summary> = {};

  for (const container of VIDEO_CONTAINERS) {
    totals[container] = await backfillContainer(blobServiceClient, container, VIDEO_CONTENT_TYPE);
  }
  totals[THUMBNAIL_CONTAINER] = await backfillContainer(
    blobServiceClient,
    THUMBNAIL_CONTAINER,
    THUMBNAIL_CONTENT_TYPE,
  );

  console.log("\nSummary:");
  for (const [container, summary] of Object.entries(totals)) {
    console.log(
      `  ${container}: scanned=${summary.scanned} updated=${summary.updated} skipped=${summary.skipped}`,
    );
  }
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
