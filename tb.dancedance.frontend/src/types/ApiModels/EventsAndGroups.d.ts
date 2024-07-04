import { EventType } from "../EventType"
import { EventId, GroupId } from "./TypeIds"

export interface IUserEventsAndGroupsResponse {
    assigned: IEventsAndGroupsResponse
    available: IEventsAndGroupsResponse
}

export interface IEventsAndGroupsResponse {
    events: Array<Event>
    groups: Array<Group>
}

export interface ICreateNewEventRequest {
    event: IEventBase
}

export interface IEventBase {
    name: string
    date: Date
    eventType: EventType
}

export interface Event extends IEventBase {
    id: EventId
}

export interface Group {
    id: GroupId
    name: string
}