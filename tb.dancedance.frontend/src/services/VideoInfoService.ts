import { DanceType } from "../types/common";
import VideoInformations from "../types/VideoInformations";
import apiClient from "./HttpApiClient";

export class VideoInfoService {
    async LoadVideos(): Promise<Array<VideoInformations>> {
        const response = await apiClient.get<Array<VideoInformations>>('/api/video/getinformations')
        return response.data
    }
}