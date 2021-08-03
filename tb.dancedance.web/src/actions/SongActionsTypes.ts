export const LOADING_SONGS = "LOADING_SONGS"
export const LOADING_SONGS_SUCCESS = "LOADING_SONGS_SUCCESS"
export const LOADING_SONGS_FAILED = "LOADING_SONGS_FAILED"

import Song from "../types/song"

export interface SongLoading {
    type: typeof LOADING_SONGS
}

export interface SongLoading_Success {
    type: typeof LOADING_SONGS_SUCCESS,
    payload: Array<Song>
}

export interface SongLoading_Failed {
    type: typeof LOADING_SONGS_FAILED
}

export type SongDispatchTypes = SongLoading | SongLoading_Success | SongLoading_Failed