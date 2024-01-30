export default interface VideoInformation {
    name: string
    recordedDateTime: Date
    id: number
    blobId: string
    duration: TimeRanges
    converted: boolean
}