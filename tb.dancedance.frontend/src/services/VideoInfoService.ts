import { BlockBlobClient } from "@azure/storage-blob";
import UploadVideoInformation from "../types/UploadInformation";
import VideoInformations from "../types/VideoInformations";
import { IAssignedEventSharingScopeModel, IEventsAndGroupsModel, ISharingScopeModel } from "../types/SharingScopeModel";
import AppApiClient from "./HttpApiClient";
import ISharedVideoInformation from "../types/ApiModels/SharedVideoInformation";


export class VideoInfoService {
    public async LoadVideos(): Promise<Array<VideoInformations>> {
        const response = await AppApiClient.get<Array<VideoInformations>>('/api/video/getinformation')
        return response.data
    }

    public GetVideoUrl(videoInfo: VideoInformations) {
        return this.GetVideUrlByBlobId(videoInfo.blobId)
    }

    public GetVideUrlByBlobId(videoBlob: string) {
        return AppApiClient.getUri() + '/api/video/stream/' + videoBlob
    }

    public async GetVideoInfo(videoId: string){
        const url = '/api/video/' + videoId + '/getinformation'
        const response = await AppApiClient.get<VideoInformations>(url)
        return response.data
    }

    public async GetAvailableEventsAndGroups() {
        const sharingScopes = await AppApiClient.get<IEventsAndGroupsModel>('/api/events/getall')
        const availableGroups = await this.GetAssignments()
        const availableGroupsMap = new Map(availableGroups.map((v) => [v.id, v]))

        const extendedModel = new Array<IAssignedEventSharingScopeModel>

        for (const el of sharingScopes.data.events) {

            const isAlreadyAssigned = availableGroupsMap.has(el.id)
            extendedModel.push({ ...el, isAssigned: isAlreadyAssigned })
        }

        return {
            events: extendedModel,
            groups: sharingScopes.data.groups
        }
    }

    public async SendAssigmentRequest(events?: Array<string>, groups?: Array<string>) {

        if (!events && !groups)
            throw new Error("Argument events or groups must me provided. Both are not provided.")

        const response = await AppApiClient.post('/api/events/requestassigment', {
            events: events,
            groups: groups
        })

        if (response.status !== 200)
            return false;

        return true;
    }

    public async GetAssignments() {
        const response = await AppApiClient.get<Array<ISharingScopeModel>>('/api/video/getassignments')
        return response.data
    }

    public async UploadVideo(data: ISharedVideoInformation, file: File, onProgress: (loadedBytes: number) => void) {

        const uploadUrl = await AppApiClient.post<UploadVideoInformation>('/api/video/getuploadurl',
            data
        );

        const containerClient = new BlockBlobClient(
            uploadUrl.data.sas
        );

        const blobBlock = containerClient.getBlockBlobClient()
        await blobBlock.uploadData(file, {
            onProgress: (e) => onProgress(e.loadedBytes)
        });
    }
}

const videoInfoService = new VideoInfoService()

export default videoInfoService;