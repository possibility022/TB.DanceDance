export default interface SharedLinkResponse {
    linkId: string
    videoId: string
    videoName: string
    createdAt: date
    expireAt: date
    isRevoked: bool
    shareUrl: string
}