export interface RequestedAccessesResponse {
    accessRequests: Array<RequestedAccess>
}

export interface RequestedAccess {
    name: string
    requestorFirstName: string
    requestorLastName: string
    requestId: string
    isGroup: boolean
    whenJoined: Date?
}

export interface ApproveAccessRequest {
    requestId: string
    isGroup: boolean
    isApproved: boolean
}