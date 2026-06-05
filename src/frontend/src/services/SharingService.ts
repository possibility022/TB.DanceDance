import { AxiosRequestConfig } from "axios";
import httpApiClient from "./HttpApiClient";
import { CreateSharedLinkRequest, SharedLinkResponse, SharedVideoInfoResponse } from "../types/ApiModels/dancedance/apiModels";
import AppApiClient from "./HttpApiClient";

class SharingService {
    shareVideo(videoId: string, allowComments: boolean, allowAnonymousComments: boolean, expirationDays?: number, signal?: AbortSignal){
        const body: CreateSharedLinkRequest = {
            expirationDays: expirationDays ?? 7,
            allowAnonymousComments: allowAnonymousComments,
            allowComments: allowComments,
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
