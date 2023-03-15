export default interface VideoInformation {
    name: string
    recordedTimeUtc: Date
    id: number
    blobId: string
    duration: TimeRanges
    MetadataAsJson: string
}