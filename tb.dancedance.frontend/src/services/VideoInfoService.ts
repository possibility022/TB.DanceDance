import { BlobServiceClient, BlockBlobClient, ContainerClient } from "@azure/storage-blob";
import UploadVideoInformation from "../types/UploadInformation";
import VideoInformations from "../types/VideoInformations";
import { apiClientFactory } from "./HttpApiClient";
import { Buffer } from "buffer";

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

    public async UploadVideo(){

        const uploadUrl = await apiClient.get<UploadVideoInformation>('/api/video/getuploadurl');

        const containerClient = new BlockBlobClient(
            uploadUrl.data.url
          );

          const client = containerClient.getBlockBlobClient()
          const buffer = Buffer.from("testing :)", 'utf-8')
          await client.uploadData(buffer)
    }
}

const videoInfoService = new VideoInfoService()

export default videoInfoService;