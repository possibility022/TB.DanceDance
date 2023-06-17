import { EventType } from "../EventType"

export interface IEventsAndGroups{
    events: Array<Event>
    groups: Array<Group>
}

export interface Event {
    id: string
    name: string
    dateTime: Date
    eventType: EventType
}

export interface Group {
    id: string
    name: string
}