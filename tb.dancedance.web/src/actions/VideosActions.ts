import { Dispatch } from "redux"
import { LOADING_VIDEOS, LOADING_VIDEOS_FAILED, LOADING_VIDEOS_SUCCESS, LOGGING_USER, VideosDispatchTypes } from "./VideosActionsTypes"
import axios from "axios"
import { Agent } from "https"
import Song from "../types/videoinformation"

export const GetVideos = (userHash: string) => async (dispatch: Dispatch<VideosDispatchTypes>):Promise<void> => {
	try {

		dispatch({
			type: LOADING_VIDEOS
		})

		const agent = new Agent({
			rejectUnauthorized: true
		})

		const url = process.env.REACT_APP_BASE_API_URL

		const res = await axios.get<Array<Song>>(url+"/api/videos",
			{httpsAgent: agent, headers: {
				"Access-Control-Allow-Headers": "*",
				"Access-Control-Allow-Origin": url,
				"userHash": userHash
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

export const LogIn = (email: string, password: string) => async (dispatch: Dispatch<VideosDispatchTypes>):Promise<void> => {
	try {
		const agent = new Agent({
			rejectUnauthorized: true
		})

		const url = process.env.REACT_APP_BASE_API_URL

		const res = await axios.post<string>(url + "/api/login",
			{
				email: email,
				password: password
			},
			{httpsAgent: agent, headers: {
				"Access-Control-Allow-Headers": "*",
				"Access-Control-Allow-Origin": url,
			}})

		let hash = ""

		if (res.status === 200){
			hash = res.data
		}

		dispatch({
			type: LOGGING_USER,
			userLoggedIn: res.status === 200,
			userLoggingHash: hash

		})
	} catch (e) {
		dispatch({
			type: LOADING_VIDEOS_FAILED
		})
	}
}