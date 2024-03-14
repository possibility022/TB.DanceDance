export interface RequestedAccessesResponse {
    accessRequests: Array<RequestedAccess>
}

export interface RequestedAccess {
    name: string
    requestorFirstName: string
    requestorLastName: string
    requestId: string
    isGroup: bool
    whenJoined: Date?
}