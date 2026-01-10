import {BlobId} from "../TypeIds";

export default interface SharedVideoInfoResponse {
    videoId: BlobId
    name: string
    duration: TimeSpan
    recordedDateTime: DateTime
    allowCommentsOnThisLink: boolean
    allowAnonymousCommentsOnThisLink: boolean
}