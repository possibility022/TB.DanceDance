import { GroupId } from "./TypeIds"
import VideoInformation from "./VideoInformation"

export interface IGroupWithVideosResponse {
    groupId: GroupId
    groupName: string
    seasonStart: Date
    seasonEnd: Date
    videos: Array<VideoInformation>
}