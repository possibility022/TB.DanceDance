import Song from "../types/song"
import { LOADING_SONGS, LOADING_SONGS_FAILED, LOADING_SONGS_SUCCESS, SongDispatchTypes } from "../actions/SongActionsTypes"

interface DefaultState {
	loading: boolean,
	songs: Array<Song>
}
const defaultState: DefaultState = {
	loading: false,
	songs: []
}
const songReducer = (state : DefaultState = defaultState, action: SongDispatchTypes) : DefaultState => {

	switch(action.type){
	case LOADING_SONGS_FAILED:
		return {
			...state,
			loading: false,
		}
	case LOADING_SONGS:
		return {
			...state,
			loading: true
		}
	case LOADING_SONGS_SUCCESS:
		return{
			...state,
			loading: false,
			songs: action.payload
		}
	}

	return state
}

export default songReducer