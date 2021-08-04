import { Dispatch } from "redux"
import { LOADING_VIDEOS, LOADING_VIDEOS_FAILED, LOADING_VIDEOS_SUCCESS, VideosDispatchTypes } from "./VideosActionsTypes"
import axios from "axios"
import { Agent } from "https"
import Song from "../types/videoinformation"

export const GetVideos = () => async (dispatch: Dispatch<VideosDispatchTypes>):Promise<void> => {
	try {

		console.log("ABC")

		dispatch({
			type: LOADING_VIDEOS
		})

		const agent = new Agent({
			rejectUnauthorized: true
		})

		const res = await axios.get<Array<Song>>("https://localhost:44328/api/videos",
			{httpsAgent: agent, headers: {
				"Access-Control-Allow-Headers": "*",
				"Access-Control-Allow-Origin": "https://localhost:44328",
			}})

		dispatch({
			type: LOADING_VIDEOS_SUCCESS,
			payload: res.data
		})
	} catch (e) {
		dispatch({
			type: LOADING_VIDEOS_FAILED
		})
	}
}