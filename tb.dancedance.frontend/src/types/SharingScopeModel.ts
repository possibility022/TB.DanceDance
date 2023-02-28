import { AssigmentType } from "./AssigmentType"
import { EventType } from "./EventType"

export interface ISharingScopeModel {
    name: string
    id: string
    assignment: AssigmentType
}

export interface IEventSharingScopeModel extends ISharingScopeModel {
    type: EventType
}

export interface IAssignedEventSharingScopeModel extends IEventSharingScopeModel {
    isAssigned: boolean
}

export interface IEventsAndGroupsModel {
    events: Array<IEventSharingScopeModel>
    groups: Array<ISharingScopeModel>
}
