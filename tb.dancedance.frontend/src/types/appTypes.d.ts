import { GroupId, EventId } from "./ApiModels/TypeIds"

export type SharedScope = {
    groupId: GroupId | null
    eventId: EventId | null
}