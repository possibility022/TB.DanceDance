export default interface VideoInformations {
    name: string
    creationTimeUtc: Date
    id: number
    blobId: string
    duration: TimeRanges
    MetadataAsJson: string
}