import Song from "../types/videoinformation"
import { LOADING_VIDEOS, LOADING_VIDEOS_FAILED, LOADING_VIDEOS_SUCCESS, VideosDispatchTypes } from "../actions/VideosActionsTypes"

interface DefaultState {
	loading: boolean,
	songs: Array<Song>
}
const defaultState: DefaultState = {
	loading: false,
	songs: []
}
const videoReducer = (state : DefaultState = defaultState, action: VideosDispatchTypes) : DefaultState => {

	switch(action.type){
	case LOADING_VIDEOS_FAILED:
		return {
			...state,
			loading: false,
		}
	case LOADING_VIDEOS:
		return {
			...state,
			loading: true
		}
	case LOADING_VIDEOS_SUCCESS:
		return{
			...state,
			loading: false,
			songs: action.payload
		}
	}

	return state
}

export default videoReducer