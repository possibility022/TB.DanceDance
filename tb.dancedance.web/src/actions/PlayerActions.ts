import { Dispatch } from "redux"
import { PlayerDispatchTypes, PLAYING_PAUSED, PLAYING_SONG, SET_SONG_INDEX, STREAMING_SONG, WAITING_FOR_STREAM } from "./PlayerActionTypes"

const SetSource = (songId: number) => async (dispatch: Dispatch<PlayerDispatchTypes>):Promise<void> => {
	try {
		dispatch({
			type: WAITING_FOR_STREAM
		})

		dispatch({
			type: STREAMING_SONG,
			src: "https://localhost:44328/api/player/" + songId
		})
	} catch (e) {
		console.log(e)
	}
}


const PlayOrPause = (play: boolean) => async (dispatch: Dispatch<PlayerDispatchTypes>):Promise<void> => {
	try {
		if (play){
			dispatch({
				type: PLAYING_SONG
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

const SetSongIndex = (songIndex: number) => async (dispatch: Dispatch<PlayerDispatchTypes>):Promise<void> => {
	dispatch({
		type: SET_SONG_INDEX,
		songIndex: songIndex
	})
}

export {SetSource as StreamSong,  PlayOrPause, SetSongIndex}
