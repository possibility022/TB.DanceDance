import ISharingScopeModel from "../SharingScopeModel"

export default interface ISharedVideoInformation{
    nameOfVideo: string
    recorded: Date
    sharedWith: ISharingScopeModel
}