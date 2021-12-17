export interface Video {
    id: string
    name: string
}

export class VideoInfoService {

    async LoadVideos(): Promise<[Video]> {
        return Promise.resolve(
            [
                {
                    id: "id",
                    name: "name"
                }
            ]

        )
    }

}