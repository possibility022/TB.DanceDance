import SharingWithType from "./SharingWithType"

export default interface ISharedVideoInformation{
    nameOfVideo: string
    fileName: string
    recordedTimeUtc: Date
    sharedWith: string
    sharingWithType: SharingWithType
}