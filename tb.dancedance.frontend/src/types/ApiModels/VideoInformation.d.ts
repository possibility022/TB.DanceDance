import { BlobId, VideoId } from "./TypeIds"

export default interface VideoInformation {
    name: string
    recordedDateTime: Date
    id: VideoId
    blobId: BlobId
    duration: TimeRanges
    converted: boolean
}