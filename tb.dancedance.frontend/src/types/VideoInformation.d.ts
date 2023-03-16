import { ISharingScopeModel } from './SharingScopeModel'
export default interface VideoInformation {
    name: string
    recordedTimeUtc: Date
    id: number
    blobId: string
    duration: TimeRanges
    metadataAsJson?: string
    sharedWith: ISharingScopeModel
}