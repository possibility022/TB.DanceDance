import Song from "../types/videoinformation"
import { LOADING_VIDEOS, LOADING_VIDEOS_FAILED, LOADING_VIDEOS_SUCCESS, LOGGING_USER, VideosDispatchTypes } from "../actions/VideosActionsTypes"

interface DefaultState {
	loading: boolean,
	userLoggedIn: boolean,
	userLoginHash: string,
	videos: Array<Song>
}
const defaultState: DefaultState = {
	loading: false,
	userLoggedIn: false,
	userLoginHash: "",
	videos: []
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
			videos: action.payload
		}
	case LOGGING_USER:
		return{
			...state,
			userLoggedIn: action.userLoggedIn,
			userLoginHash: action.userLoggingHash
		}
	}
	return state
}

export default videoReducer