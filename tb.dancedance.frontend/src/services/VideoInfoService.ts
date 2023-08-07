import { BlockBlobClient } from "@azure/storage-blob";
import UploadVideoInformation from "../types/ApiModels/UploadInformation";
import AppApiClient from "./HttpApiClient";
import ISharedVideoInformation from "../types/ApiModels/SharedVideoInformation";
import VideoInformation from "../types/VideoInformation";
import { IEventsAndGroups, ICreateNewEventRequest, Event } from "../types/ApiModels/EventsAndGroups";
import { IAssignedEvent, IAssignedGroup } from "../types/AssignedEventAndGroup";


export class VideoInfoService {
    public async LoadVideos(): Promise<Array<VideoInformation>> {
        const response = await AppApiClient.get<Array<VideoInformation>>('/api/videos')
        return response.data
    }

    public GetVideoUrl(videoInfo: VideoInformation) {
        return this.GetVideUrlByBlobId(videoInfo.blobId)
    }

    public async GetVideosFromGroups() {
        const response = await AppApiClient.get<Array<VideoInformation>>('/api/groups/videos')
        return response.data
    }

    public GetVideUrlByBlobId(videoBlob: string) {
        return AppApiClient.getUri() + '/api/videos/' + videoBlob + '/stream'
    }

    public async GetVideoInfo(videoId: string){
        const url = '/api/videos/' + videoId
        const response = await AppApiClient.get<VideoInformation>(url)
        return response.data
    }

    public async GetAvailableEventsAndGroups() {
        const allGroupsAndEvents = await AppApiClient.get<IEventsAndGroups>('/api/videos/accesses')
        const userGroupAndEvents = await this.GetUserEventsAndGroups()
        
        const availableEventsMap = new Map(userGroupAndEvents.events.map(v => [v.id, v]))
        const availableGroupsMap = new Map(userGroupAndEvents.groups.map(v => [v.id, v]))

        const events = new Array<IAssignedEvent>()
        const groups = new Array<IAssignedGroup>()

        for (const el of allGroupsAndEvents.data.events) {

            const isAlreadyAssigned = availableEventsMap.has(el.id)
            events.push({ ...el, isAssigned: isAlreadyAssigned })
        }

        for (const el of allGroupsAndEvents.data.groups) {

            const isAlreadyAssigned = availableGroupsMap.has(el.id)
            groups.push({ ...el, isAssigned: isAlreadyAssigned })
        }

        return {
            events: events,
            groups: groups
        }
    }

    public async SendAssigmentRequest(events?: Array<string>, groups?: Array<string>) {

        if (!events && !groups)
            throw new Error("Argument events or groups must me provided. Both are not provided.")

        const response = await AppApiClient.post('/api/videos/accesses/request', {
            events: events,
            groups: groups
        })

        if (response.status !== 200)
            return false;

        return true;
    }

    public async GetUserEventsAndGroups() {
        const response = await AppApiClient.get<IEventsAndGroups>('/api/videos/accesses/my')
        return response.data
    }

    public async CreateEvent(newEvent: ICreateNewEventRequest) {
        const response = await AppApiClient.post<Event>('/api/events', newEvent)
        return {
            statusCode: response.status,
            eventObject: response.data
        }
    }

    public async GetVideosPerEvent(eventId: string){
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
}

const videoInfoService = new VideoInfoService()

export default videoInfoService;