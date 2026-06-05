import { BlockBlobClient } from "@azure/storage-blob";
import AppApiClient from "./HttpApiClient";
import {
    VideoInformation,
    VideoFromGroupInformation,
    ListGroupVideosResponse,
    MyVideosResponse,
    ListEventVideosResponse,
    VideoInformationResponse,
    CreateNewEventRequest,
    CreateNewEventResponse,
    GetUserAccessResponse,
    ListAccessRequestsResponse,
    RequestedAccessModel,
    ApproveAccessRequestRequest,
    RequestAccessRequest,
    ProduceUploadUrlRequest,
    ProduceUploadUrlResponse,
    UpdateCommentSettingsRequest,
} from "../types/ApiModels/dancedance/apiModels";
import { AxiosResponse } from "axios";


export class VideoInfoService {

    public GetVideoUrl(videoInfo: VideoInformation) {
        return this.GetVideUrlByBlobId(videoInfo.blobId!)
    }

    public async GetVideosFromGroups() {
        const response = await AppApiClient.get<ListGroupVideosResponse>('/api/groups/videos')
        return response.data.videos ?? []
    }

    public async GetVideosForGroup(groupId: string) {
        const response = await AppApiClient.get<ListGroupVideosResponse>('/api/groups/' + groupId + '/videos')
        return response.data.videos ?? []
    }

    public GetVideUrlByBlobId(videoBlob: string) {
        return AppApiClient.getUri() + '/api/videos/' + videoBlob + '/stream'
    }

    public async GetVideoInfo(videoId: string) {
        const url = '/api/videos/' + videoId
        const response = await AppApiClient.get<VideoInformationResponse>(url)
        return response.data.videoInformation!
    }

    public async GetAvailableEventsAndGroups() {
        const userGroupAndEvents = await this.GetUserEventsAndGroups()
        return userGroupAndEvents
    }

    public async SendAssigmentRequest(requestModel: RequestAccessRequest) {

        if (!requestModel.events && !requestModel.groups)
            throw new Error("Argument events or groups must me provided. Both are not provided.")

        const response = await AppApiClient.post('/api/videos/accesses/request', requestModel)

        if (response.status < 200 || response.status > 299)
            return false;

        return true;
    }

    public async GetUserEventsAndGroups() {
        const response = await AppApiClient.get<GetUserAccessResponse>('/api/videos/accesses/my')
        return response.data
    }

    public async CreateEvent(newEvent: CreateNewEventRequest) {
        const response = await AppApiClient.post<CreateNewEventResponse>('/api/events', newEvent)
        return response.data
    }

    public async GetVideosForEvent(eventId: string) {
        const response = await AppApiClient.get<ListEventVideosResponse>(`/api/events/${eventId}/videos`)

        if (response.status > 299)
            console.error('Videos not received', response)

        return response.data.videos ?? []
    }

    public async UploadVideo(data: ProduceUploadUrlRequest, file: File, onProgress: (loadedBytes: number) => void) {

        const uploadUrl = await AppApiClient.post<ProduceUploadUrlResponse>('/api/videos/upload', data);

        const containerClient = new BlockBlobClient(uploadUrl.data.sas!)

        const blobBlock = containerClient.getBlockBlobClient()
        await blobBlock.uploadData(file, {
            onProgress: (e) => onProgress(e.loadedBytes)
        });
    }

    public async RenameVideo(videoId: string, newName: string) {
        const response = await AppApiClient.post(`/api/videos/${videoId}/rename`, { newName })
        this.EnsureSuccessStatusCode(response)
        return true
    }

    public async GetAccessRequests() {
        const response = await AppApiClient.get<ListAccessRequestsResponse>('/api/videos/accesses/requests')
        this.EnsureSuccessStatusCode(response)

        return response.data;
    }

    public async GetPrivateVideos(){
        const response = await AppApiClient.get<MyVideosResponse>(`/api/videos/my`)
        this.EnsureSuccessStatusCode(response)
        return response.data.videoInformation ?? []
    }

    public async RejectAccessRequest(request: RequestedAccessModel) {
        await this.SendAccessRequestAction(request, false)
    }

    public async ApproveAccessRequest(request: RequestedAccessModel) {
        await this.SendAccessRequestAction(request, true)
    }

    private async SendAccessRequestAction(request: RequestedAccessModel, approved: boolean) {
        const requestBody: ApproveAccessRequestRequest = {
            requestId: request.requestId,
            isGroup: request.isGroup,
            isApproved: approved
        }
        const response = await AppApiClient.post('/api/videos/accesses/requests', requestBody)
        return response.status >= 200 && response.status < 300
    }

    EnsureSuccessStatusCode(response: AxiosResponse) {
        if (response.status > 299 || response.status < 200)
            throw new Error('Request not accepted.')
    }

    async ChangeCommentsVisibility(videoId: string, number: number) {
        const requestBody: UpdateCommentSettingsRequest = {
            commentVisibility: number
        }
        const response = await AppApiClient.put(`/api/videos/${videoId}/comment-settings`, requestBody)
        return response.status >= 200 && response.status < 300
    }
}

const videoInfoService = new VideoInfoService()

export default videoInfoService;
