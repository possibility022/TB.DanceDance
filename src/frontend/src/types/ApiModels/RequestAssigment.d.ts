interface PostRequestAssigmentRequest {
    events: string[] | undefined
    groups: GroupAssigmentModel[] | undefined
}

interface GroupAssigmentModel {
    id: string
    joinedDate: Date
}