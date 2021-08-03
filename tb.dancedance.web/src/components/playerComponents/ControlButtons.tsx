import * as React from "react"

export interface IControlButtonsProps {
    isPlaying: boolean
    setIsPlaying: (playing: boolean) => void
    skipSong: (forward: boolean) => void
}

export default function controlButtons(props: IControlButtonsProps): JSX.Element {
	return (
		<div>
			<button onClick={() => props.skipSong(false)}>
				<svg id="i-start" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32" width="32" height="32" fill="none" stroke="currentcolor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="2">
					<path d="M8 2 L8 16 22 2 22 30 8 16 8 30" />
				</svg>
			</button>
			<button onClick={() => props.setIsPlaying(!props.isPlaying)}>
				{props.isPlaying ?
					<svg id="i-pause" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32" width="32" height="32" fill="none" stroke="currentcolor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="2">
						<path d="M23 2 L23 30 M9 2 L9 30" />
					</svg>
					:
					<svg id="i-play" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32" width="32" height="32" fill="none" stroke="currentcolor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="2">
						<path d="M10 2 L10 30 24 16 Z" />
					</svg>
				}
			</button>
			<button onClick={() => props.skipSong(true)}>
				<svg id="i-end" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32" width="32" height="32" fill="none" stroke="currentcolor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="2">
					<path d="M24 2 L24 16 10 2 10 30 24 16 24 30" />
				</svg>
			</button>
		</div>
	)
}
