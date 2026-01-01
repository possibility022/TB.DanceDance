import { AxiosRequestConfig } from "axios";
import httpApiClient from "./HttpApiClient";
import CreateSharedLinkRequest from "../types/ApiModels/Sharing/CreateSharedLinkRequest";
import SharedLinkResponse from "../types/ApiModels/Sharing/SharedLinkResponse";
import SharedVideoInfoResponse from "../types/ApiModels/Sharing/SharedVideoInfoResponse";
import AppApiClient from "./HttpApiClient";

class SharingService {
    shareVideo(videoId: string, expirationDays?: number, signal?: AbortSignal){
        const body: CreateSharedLinkRequest = {
            ExpirationDays: expirationDays ?? 7
        }

        const config: AxiosRequestConfig = {
            signal: signal
        }

        return httpApiClient.post<SharedLinkResponse>(`api/videos/${videoId}/share`, body, config)
    }

    getVideoInformationFromLink(linkId: string) {
        return httpApiClient.get<SharedVideoInfoResponse>(`api/share/${linkId}`)
    }

    getVideUrlByLinkId(linkId: string) {
        return AppApiClient.getUri() + `/api/share/${linkId}/stream`
    }
}

const sharingService = new SharingService();

export default sharingService;