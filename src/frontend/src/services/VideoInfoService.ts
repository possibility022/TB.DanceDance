import { BlockBlobClient } from "@azure/storage-blob";
import UploadVideoInformation from "../types/ApiModels/UploadInformation";
import AppApiClient from "./HttpApiClient";
import ISharedVideoInformation from "../types/ApiModels/SharedVideoInformation";
import VideoInformation from "../types/ApiModels/VideoInformation";
import { ICreateNewEventRequest, Event, IUserEventsAndGroupsResponse } from "../types/ApiModels/EventsAndGroups";
import IRenameRequest from "../types/ApiModels/VideoRenameRequest";
import { IGroupWithVideosResponse } from "../types/ApiModels/GroupsWithVideosResponse";
import { ApproveAccessRequest, RequestedAccess, RequestedAccessesResponse } from "../types/ApiModels/RequestedAccessesResponse";
import { AxiosResponse } from "axios";
import { EventId, GroupId, VideoId } from "../types/ApiModels/TypeIds";
import {UpdateVideoCommentSettingsRequest} from "../types/ApiModels/UpdateVideoCommentSettingsRequest";


export class VideoInfoService {

    public GetVideoUrl(videoInfo: VideoInformation) {
        return this.GetVideUrlByBlobId(videoInfo.blobId)
    }

    public async GetVideosFromGroups() {
        const response = await AppApiClient.get<Array<IGroupWithVideosResponse>>('/api/groups/videos')
        return response.data
    }

    public async GetVideosForGroup(groupId: GroupId) {
        const response = await AppApiClient.get<IGroupWithVideosResponse>('/api/groups/' + groupId + '/videos')
        return response.data
    }

    public GetVideUrlByBlobId(videoBlob: string) {
        return AppApiClient.getUri() + '/api/videos/' + videoBlob + '/stream'
    }

    public async GetVideoInfo(videoId: string) {
        const url = '/api/videos/' + videoId
        const response = await AppApiClient.get<VideoInformation>(url)
        return response.data
    }

    public async GetAvailableEventsAndGroups() {
        const userGroupAndEvents = await this.GetUserEventsAndGroups()
        return userGroupAndEvents
    }

    public async SendAssigmentRequest(requestModel: PostRequestAssigmentRequest) {

        if (!requestModel.events && !requestModel.groups)
            throw new Error("Argument events or groups must me provided. Both are not provided.")

        const response = await AppApiClient.post('/api/videos/accesses/request', requestModel)

        if (response.status !== 200)
            return false;

        return true;
    }

    public async GetUserEventsAndGroups() {
        const response = await AppApiClient.get<IUserEventsAndGroupsResponse>('/api/videos/accesses/my')
        return response.data
    }

    public async CreateEvent(newEvent: ICreateNewEventRequest) {
        const response = await AppApiClient.post<Event>('/api/events', newEvent)
        return {
            statusCode: response.status,
            eventObject: response.data
        }
    }

    public async GetVideosForEvent(eventId: EventId) {
        const response = await AppApiClient.get<Array<VideoInformation>>(`/api/events/${eventId}/videos`)

        if (response.status > 299)
            console.error('Videos not received', response)

        return response.data
    }

    public async UploadVideo(data: ISharedVideoInformation, file: File, onProgress: (loadedBytes: number) => void) {

        const uploadUrl = await AppApiClient.post<UploadVideoInformation>('/api/videos/upload',
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

    public async RenameVideo(videoId: VideoId, newName: string) {
        const requestBody: IRenameRequest = {
            newName: newName
        }

        const response = await AppApiClient.post(`/api/videos/${videoId}/rename`, requestBody)
        this.EnsureSuccessStatusCode(response)
        return true
    }

    public async GetAccessRequests() {
        const response = await AppApiClient.get<RequestedAccessesResponse>('/api/videos/accesses/requests')
        this.EnsureSuccessStatusCode(response)

        return response.data;
    }

    public async GetPrivateVideos(){
        const response = await AppApiClient.get<Array<VideoInformation>>(`/api/videos/my`)
        this.EnsureSuccessStatusCode(response)
        return response.data
    }

    public async RejectAccessRequest(request: RequestedAccess) {
        await this.SendAccessRequestAction(request, false)
    }

    public async ApproveAccessRequest(request: RequestedAccess) {
        await this.SendAccessRequestAction(request, true)
    }

    private async SendAccessRequestAction(request: RequestedAccess, approved: boolean) {
        const requestBody: ApproveAccessRequest = {
            requestId: request.requestId,
            isGroup: request.isGroup,
            isApproved: approved
        }
        const response = await AppApiClient.post('/api/videos/accesses/requests', requestBody)
        return response.status > 200 && response.status < 299
    }

    EnsureSuccessStatusCode(response: AxiosResponse) {
        if (response.status > 299 || response.status < 200)
            throw new Error('Request not accepted.')
    }

    async ChangeCommentsVisibility(videoId: VideoId, number: number) {
        const requestBody: UpdateVideoCommentSettingsRequest = {
            commentVisibility: number
        }
        const response = await AppApiClient.put(`/api/videos/${videoId}/comment-settings`, requestBody)
        return response.status > 200 && response.status < 299
    }
}

const videoInfoService = new VideoInfoService()

export default videoInfoService;