import { Dispatch } from "redux"
import VideoInformation from "../types/videoinformation"
import { PlayerDispatchTypes, PLAYING_PAUSED, PLAYING_VIDEO, SET_VIDEO, STREAMING_VIDEO, WAITING_FOR_STREAM } from "./PlayerActionTypes"

const SetSource = (videoBlobId: string, userHash: string) => async (dispatch: Dispatch<PlayerDispatchTypes>):Promise<void> => {
	try {
		dispatch({
			type: WAITING_FOR_STREAM
		})

		const url = process.env.REACT_APP_BASE_API_URL

		dispatch({
			type: STREAMING_VIDEO,
			src: url + "/api/stream/" + videoBlobId + "?userHash=" + userHash
		})
	} catch (e) {
		console.log(e)
	}
}

const PlayOrPause = (play: boolean) => async (dispatch: Dispatch<PlayerDispatchTypes>):Promise<void> => {
	try {
		if (play){
			dispatch({
				type: PLAYING_VIDEO
			})
		} else {
			dispatch({
				type: PLAYING_PAUSED
			})
		}
	} catch (e) {
		console.log(e)
	}
}

const SetVideoIndex = (videoInformation: VideoInformation) => async (dispatch: Dispatch<PlayerDispatchTypes>):Promise<void> => {
	dispatch({
		type: SET_VIDEO,
		videoInformation: videoInformation
	})
}

export {SetSource,  PlayOrPause, SetVideoIndex}
