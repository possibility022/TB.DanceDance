import { PlayerDispatchTypes, PLAYING_PAUSED, PLAYING_SONG, SET_SONG_INDEX, STREAMING_SONG, WAITING_FOR_STREAM } from "../actions/PlayerActionTypes"

interface PLayerState {
	playing: boolean,
	src:  string | undefined,
	songIndex: number | undefined
}
const defaultState: PLayerState = {
	playing: false,
	src: undefined,
	songIndex: undefined
}
const playerReducer = (state : PLayerState = defaultState, action: PlayerDispatchTypes) : PLayerState => {

	switch(action.type){
	case STREAMING_SONG:
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
	case PLAYING_SONG:
		return {
			...state,
			playing: true
		}
	case SET_SONG_INDEX:
		return {
			...state,
			songIndex: action.songIndex
		}
	}
	return state
}

export default playerReducer