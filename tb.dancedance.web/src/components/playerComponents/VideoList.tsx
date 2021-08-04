import * as React from "react"
import { useDispatch, useSelector } from "react-redux"
import { GetVideos } from "../../actions/VideosActions"
import { RootState } from "../../store/configureStore"

export default function VideoList (): JSX.Element {

	const songState = useSelector((state: RootState) => state.song.songs)
	const loading = useSelector((state: RootState) => state.song.loading)
	const dispatch = useDispatch()
	const loadVideos = () => dispatch(GetVideos())

	const renderSongs = songState.map((video) => {
		return (<tr key={video.id}>
			<td>{video.id}</td>
			<td>{video.name}</td>
			<td>{video.blobId}</td>
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

			{loading === false &&
			<button onClick={loadVideos}>Reload</button>
			}

			<table>
				<thead>
					<tr>
						<th>Id</th>
						<th>Name</th>
						<th>Blob Id</th>
					</tr>
				</thead>
				<tbody>

					{renderSongs}

				</tbody>
			</table>
		</div>
	)
}
