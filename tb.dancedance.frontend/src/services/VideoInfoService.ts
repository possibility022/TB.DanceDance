import VideoInformations from "../types/VideoInformations";
import { apiClientFactory } from "./HttpApiClient";

const apiClient = apiClientFactory()

export class VideoInfoService {
    public async LoadVideos(): Promise<Array<VideoInformations>> {
        const response = await apiClient.get<Array<VideoInformations>>('/api/video/getinformations')
        return response.data
    }

    public GetVideoUrl(videoInfo: VideoInformations) {
        return this.GetVideUrlByBlobId(videoInfo.blobId)
    }

    public GetVideUrlByBlobId(videoBlob: string) {
        return apiClient.getUri() + '/api/video/stream/' + videoBlob
    }
}