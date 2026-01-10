    export type CommentResponse =
    {
        id: uuid
        videoId: uuid
        authorName?: string
        isAnonymous: boolean
        content: string
        createdAt: Date
        updatedAt?: Date | null
        isHidden?: boolean | null
        postedAsAnonymous?: boolean
        isReported?: boolean | null
        reportedReason?: string | null
        isOwn: boolean
        canModerate: boolean
    }