import { VideoId } from "./TypeIds"

export default interface VideoInformation {
    name: string
    recordedDateTime: Date
    id: VideoId
    blobId: string
    duration: TimeRanges
    converted: boolean
}