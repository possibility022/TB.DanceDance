import ControlButtons from "./playerComponents/ControlButtons"
import { useRef, useEffect, useState, SetStateAction } from "react"
// eslint-disable-next-line @typescript-eslint/no-var-requires
// const Video = require("react-video-stream") // todo - do something with that in future.
import { Video } from "react-video-stream"
import React from "react"
import VideoList from "./playerComponents/VideoList"
import { useDispatch, useSelector } from "react-redux"
import { RootState } from "../store/configureStore"
import {PlayOrPause, SetVideoIndex, SetSource } from "../actions/PlayerActions"
import VideoInformation from "../types/videoinformation"
import ReactPlayer from "react-player"

export default function Player(): JSX.Element {

	const isPlaying = useSelector((state: RootState) => state.player.playing)
	const source = useSelector((state: RootState) => state.player.src)
	const video = useSelector((state: RootState) => state.player.video)
	const [videoId, setVideoId] = useState(-1)
	const [played, setPlayed] = useState(0.0)

	const [reactPlayer, setReactPlayer] = useState<ReactPlayer|undefined>()

	const videoRef = useRef() as React.MutableRefObject<HTMLVideoElement>

	const dispatch = useDispatch()
	const setSource = (blobId: string) => { dispatch(SetSource(blobId))}
	const playOrPause = (playing: boolean) => { dispatch(PlayOrPause(playing))}

	useEffect(() => {
		if (null !== video && video !== undefined && videoId != video.id)
		{
			setSource(video?.blobId)
			setVideoId(video.id)
			if (videoRef.current) {
				videoRef.current.load()
			}
		}
	})

	const skipSong = (forwards = true) => {

		// if (songIndex === undefined)
		// 	return

		// if (forwards) {
		// 	setSong(songIndex + 1)
		// } else {
		// 	setSong(songIndex - 1)
		// }
	}

	const handleSeekChange = (e: any) => {
		console.log(e)
		setPlayed(parseFloat(e.target.value))

		if (playerRef !== undefined){

			const c = playerRef
			if (c.current !== undefined){
				c.current.seekTo(parseFloat(e.target.value))
			}
		}
	}

	const playerRef = useRef<any>()

	return (
		<div>
			{/* <audio src={props.songs[props.currentSongIndex].src} ref={audioEl}></audio> */}

			<div>
				<ReactPlayer ref={playerRef} url={source} key={source} controls={true} played={played}></ReactPlayer>
			</div>

			<input
				type='range' min={0} max={0.999999} step='any'
				value={played}
				onChange={handleSeekChange}
			/>

			{/* <video src={source} ref={audioEl}></audio> */}
			{/* <h3>{songIndex !== undefined && songs[songIndex]?.name}</h3>
			<h4>{songIndex !== undefined && songs[songIndex]?.blobId}</h4> */}
			<ControlButtons
				isPlaying={isPlaying}
				setIsPlaying={playOrPause}
				skipSong={skipSong}
			></ControlButtons>
			<div>
				<VideoList/>
			</div>
		</div>
	)
}
