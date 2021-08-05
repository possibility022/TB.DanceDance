import { PlayerDispatchTypes, PLAYING_PAUSED, PLAYING_VIDEO, SET_VIDEO, STREAMING_VIDEO, WAITING_FOR_STREAM } from "../actions/PlayerActionTypes"
import VideoInformation from "../types/videoinformation"

interface PlayerState {
	playing: boolean,
	src:  string | undefined,
	video: VideoInformation | undefined
}
const defaultState: PlayerState = {
	playing: false,
	src: undefined,
	video: undefined
}
const playerReducer = (state : PlayerState = defaultState, action: PlayerDispatchTypes) : PlayerState => {

	switch(action.type){
	case STREAMING_VIDEO:
		return {
			...state,
			playing: true,
			src: action.src
		}

	case PLAYING_PAUSED:
	case WAITING_FOR_STREAM:
		return {
			...state,
			playing: false
		}
	case PLAYING_VIDEO:
		return {
			...state,
			playing: true
		}
	case SET_VIDEO:
		return {
			...state,
			video: action.videoInformation
		}
	}
	return state
}

export default playerReducer