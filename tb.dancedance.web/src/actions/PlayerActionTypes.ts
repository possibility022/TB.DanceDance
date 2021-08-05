import VideoInformation from "../types/videoinformation"

export const STREAMING_VIDEO = "STREAMING_VIDEO"
export const WAITING_FOR_STREAM = "WAITING_FOR_STREAM"
export const PLAYING_VIDEO = "PLAYING_VIDEO"
export const PLAYING_PAUSED = "PLAYING_PAUSED"
export const SET_VIDEO = "SET_VIDEO"

export interface StreamingVideo {
    type: typeof STREAMING_VIDEO,
    src: string
}

export interface WaitingForStream {
    type: typeof WAITING_FOR_STREAM
}

export interface PlayingVideo {
    type: typeof PLAYING_VIDEO
}

export interface PlayingPaused {
    type: typeof PLAYING_PAUSED
}

export interface SetVideoIndex {
    type: typeof SET_VIDEO
    videoInformation: VideoInformation
}
export type PlayerDispatchTypes = StreamingVideo | WaitingForStream | PlayingVideo | PlayingPaused | SetVideoIndex