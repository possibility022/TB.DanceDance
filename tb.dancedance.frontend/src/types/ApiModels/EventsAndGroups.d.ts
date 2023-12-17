import { EventType } from "../EventType"

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
    id: string
}

export interface Group {
    id: string
    name: string
}