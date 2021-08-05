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

export default function Player(): JSX.Element {

	const isPlaying = useSelector((state: RootState) => state.player.playing)
	const source = useSelector((state: RootState) => state.player.src)
	const video = useSelector((state: RootState) => state.player.video)
	const [videoId, setVideoId] = useState(-1)

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

	return (
		<div>
			{/* <audio src={props.songs[props.currentSongIndex].src} ref={audioEl}></audio> */}

			<div>
				<video ref={videoRef} controls>
					<source src={source} type="video/mp4"/>
				</video>
				<Video
					className='video-class'
					controls={true}
					autoPlay={true}
					remoteUrl={source}
				/>
			</div>

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
