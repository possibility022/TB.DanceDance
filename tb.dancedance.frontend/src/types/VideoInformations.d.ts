export default interface VideoInformations {
    name: string
    recordedTimeUtc: Date
    id: number
    blobId: string
    duration: TimeRanges
    MetadataAsJson: string
}