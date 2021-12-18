import { DanceType } from "../types/common";
import VideoInformations from "../types/VideoInformations";


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

        return promise

    }

}