export const STREAMING_SONG = "STREAMING_SONG"
export const WAITING_FOR_STREAM = "WAITING_FOR_STREAM"
export const PLAYING_SONG = "PLAYING_SONG"
export const PLAYING_PAUSED = "PLAYING_PAUSED"
export const SET_SONG_INDEX = "SET_SONG_INDEX"

export interface StreamingSong {
    type: typeof STREAMING_SONG,
    src: string
}

export interface WaitingForStream {
    type: typeof WAITING_FOR_STREAM
}

export interface PlayingSong {
    type: typeof PLAYING_SONG
}

export interface PlayingPaused {
    type: typeof PLAYING_PAUSED
}

export interface SetSongIndex {
    type: typeof SET_SONG_INDEX
    songIndex: number
}
export type PlayerDispatchTypes = StreamingSong | WaitingForStream | PlayingSong | PlayingPaused | SetSongIndex