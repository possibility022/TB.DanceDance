import { Event, Group } from "./ApiModels/EventsAndGroups"

export interface IAssignedEvent extends Event {
    isAssigned: boolean
}

export interface IAssignedGroup extends Group {
    isAssigned: boolean
}