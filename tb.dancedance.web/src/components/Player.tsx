import ControlButtons from "./playerComponents/ControlButtons"
import { useRef, useEffect, useState, SetStateAction } from "react"
import Song from "../types/song"
import React from "react"
import SongsList from "./playerComponents/SongsList"
import { useDispatch, useSelector } from "react-redux"
import { RootState } from "../store/configureStore"
import {PlayOrPause, SetSongIndex, StreamSong } from "../actions/PlayerActions"

export default function Player(): JSX.Element {

	const audioEl = useRef<HTMLAudioElement>(null)
	const isPlaying = useSelector((state: RootState) => state.player.playing)
	const source = useSelector((state: RootState) => state.player.src)
	const songs = useSelector((state: RootState) => state.song.songs)
	const songIndex = useSelector((state: RootState) => state.player.songIndex)

	const dispatch = useDispatch()
	const setSource = (songId: number) => { dispatch(StreamSong(songId))}
	const playOrPause = (playing: boolean) => { dispatch(PlayOrPause(playing))}
	const setSong = (songIndex: number) => dispatch(SetSongIndex(songIndex))

	useEffect(() => {
		if (null !== audioEl.current && source !== undefined)
			if (isPlaying) {
				audioEl.current.play()
			} else {
				audioEl.current.pause()
			}
	})

	const skipSong = (forwards = true) => {

		if (songIndex === undefined)
			return

		if (forwards) {
			setSong(songIndex + 1)
		} else {
			setSong(songIndex - 1)
		}
	}

	return (
		<div>
			{/* <audio src={props.songs[props.currentSongIndex].src} ref={audioEl}></audio> */}
			<audio src={source} ref={audioEl}></audio>
			<h3>{songIndex !== undefined && songs[songIndex]?.title}</h3>
			<h4>{songIndex !== undefined && songs[songIndex]?.artist}</h4>
			<ControlButtons
				isPlaying={isPlaying}
				setIsPlaying={playOrPause}
				skipSong={skipSong}
			></ControlButtons>
			<div>
				<SongsList/>
			</div>
		</div>
	)
}
