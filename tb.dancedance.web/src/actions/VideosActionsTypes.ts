export const LOADING_VIDEOS = "LOADING_VIDEOS"
export const LOADING_VIDEOS_SUCCESS = "LOADING_VIDEOS_SUCCESS"
export const LOADING_VIDEOS_FAILED = "LOADING_VIDEOS_FAILED"
export const LOGGING_USER = "LOGGING_USER"

import Song from "../types/videoinformation"

export interface VideosLoading {
    type: typeof LOADING_VIDEOS
}

export interface VideosLoading_Success {
    type: typeof LOADING_VIDEOS_SUCCESS,
    payload: Array<Song>
}

export interface VideosLoading_Failed {
    type: typeof LOADING_VIDEOS_FAILED
}

export interface UserLoggingIn {
    type: typeof LOGGING_USER,
    userLoggedIn: boolean
    userLoggingHash: string
}

export type VideosDispatchTypes = VideosLoading | VideosLoading_Success | VideosLoading_Failed | UserLoggingIn