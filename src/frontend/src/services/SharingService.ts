import { AxiosRequestConfig } from "axios";
import httpApiClient from "./HttpApiClient";
import CreateSharedLinkRequest from "../types/ApiModels/Sharing/CreateSharedLinkRequest";
import SharedLinkResponse from "../types/ApiModels/Sharing/SharedLinkResponse";

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
}

const sharingService = new SharingService();

export default sharingService;