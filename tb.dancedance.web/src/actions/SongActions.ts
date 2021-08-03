import { Dispatch } from "redux"
import { LOADING_SONGS, LOADING_SONGS_FAILED, LOADING_SONGS_SUCCESS, SongDispatchTypes } from "./SongActionsTypes"
import axios from "axios"
import { Agent } from "https"
import Song from "../types/song"

export const GetSongs = () => async (dispatch: Dispatch<SongDispatchTypes>):Promise<void> => {
	try {
		dispatch({
			type: LOADING_SONGS
		})

		const agent = new Agent({
			rejectUnauthorized: true
		})

		const res = await axios.get<Array<Song>>("https://localhost:44367/api/songs",
			{httpsAgent: agent, headers: {
				"Access-Control-Allow-Headers": "*",
				"Access-Control-Allow-Origin": "https://localhost:44367",
			}})

		dispatch({
			type: LOADING_SONGS_SUCCESS,
			payload: res.data
		})
	} catch (e) {
		dispatch({
			type: LOADING_SONGS_FAILED
		})
	}
}