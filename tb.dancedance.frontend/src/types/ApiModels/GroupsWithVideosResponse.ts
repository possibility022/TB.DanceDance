import VideoInformation from "./VideoInformation"

export interface IGroupWithVideosResponse {
    groupId: string
    groupName: string
    videos: Array<VideoInformation>
}