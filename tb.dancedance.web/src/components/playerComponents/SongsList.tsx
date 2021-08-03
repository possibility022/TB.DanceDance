import * as React from "react"
import { useSelector } from "react-redux"
import { RootState } from "../../store/configureStore"

export default function SongsList (): JSX.Element {

	const songState = useSelector((state: RootState) => state.song.songs)
	const loading = useSelector((state: RootState) => state.song.loading)

	const renderSongs = songState.map((song) => {
		return (<tr key={song.id}>
			<td>{song.id}</td>
			<td>{song.artist}</td>
			<td>{song.title}</td>
			<td>
				<svg id="i-play" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32" width="32" height="32" fill="none" stroke="currentcolor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="2">
					<path d="M10 2 L10 30 24 16 Z" />
				</svg>
			</td>
		</tr>)
	})

	return (
		<div>

			Loading: {loading.toString()}

			<table>
				<thead>
					<tr>
						<th>Id</th>
						<th>Artist</th>
						<th>Title</th>
						<th>Play</th>
					</tr>
				</thead>
				<tbody>

					{renderSongs}

				</tbody>
			</table>
		</div>
	)
}
