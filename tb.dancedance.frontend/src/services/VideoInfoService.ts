import { DanceType } from "../types/common";
import VideoInformations from "../types/VideoInformations";
import apiClient from "./HttpApiClient";

interface VideoInfo {
    videoName: string
    videoId: string
}

export class VideoInfoService {
    async LoadVideos(): Promise<Array<VideoInformations>> {
        const promise =
            new Promise<Array<VideoInformations>>((resolve) => {
                resolve([
                    {
                        danceType: DanceType.Salsa,
                        id: 1,
                        name: "name1",
                        recordingDate: new Date(Date.now())
                    },
                    {
                        danceType: DanceType.Salsa,
                        id: 2,
                        name: "name2",
                        recordingDate: new Date(Date.now())
                    },
                    {
                        danceType: DanceType.Salsa,
                        id: 3,
                        name: "name3",
                        recordingDate: new Date(Date.now())
                    },
                    {
                        danceType: DanceType.Salsa,
                        id: 4,
                        name: "name4",
                        recordingDate: new Date(Date.now())
                    },
                    {
                        danceType: DanceType.Salsa,
                        id: 5,
                        name: "name5",
                        recordingDate: new Date(Date.now())
                    }
                ]);
            });
        // const token = this.auth.userData?.access_token
        // console.log(token)
        return promise

    }

    async LoadInformation(): Promise<Array<VideoInfo>> {
        const response = await apiClient.get<Array<VideoInfo>>('/api/video/getinformations')
        return response.data
    }

}