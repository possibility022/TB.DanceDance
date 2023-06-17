import SharingWithType from "./SharingWithType"

export default interface ISharedVideoInformation{
    nameOfVideo: string
    recordedTimeUtc: Date
    sharedWith: string
    sharingWithType: SharingWithType
}