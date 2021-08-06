import * as React from "react"
import { useDispatch, useSelector } from "react-redux"
import { SetVideoIndex } from "../../actions/PlayerActions"
import { GetVideos } from "../../actions/VideosActions"
import { RootState } from "../../store/configureStore"
import VideoInformation from "../../types/videoinformation"

export default function VideoList (): JSX.Element {

	const videoState = useSelector((state: RootState) => state.video.videos)
	const loading = useSelector((state: RootState) => state.video.loading)
	const userHash = useSelector((state: RootState) => state.video.userLoginHash)
	const dispatch = useDispatch()
	const loadVideos = () => dispatch(GetVideos(userHash))
	const playVideo = (videoId: VideoInformation) => dispatch(SetVideoIndex(videoId))

	const renderList = videoState.map((video) => {
		return (<tr key={video.id}>
			<td>{video.id}</td>
			<td>{video.name}</td>
			<td>{video.blobId}</td>
			<td>
				<button onClick={() => playVideo(video)}>
					<svg id="i-play" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32" width="32" height="32" fill="none" stroke="currentcolor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="2">
						<path d="M10 2 L10 30 24 16 Z" />
					</svg>
				</button>
			</td>
		</tr>)
	})

	return (
		<div>

			<div>
			Loading: {loading.toString()}
			</div>
			{loading === false &&
			<button onClick={loadVideos}>Reload</button>
			}

			<table>
				<thead>
					<tr>
						<th>Id</th>
						<th>Name</th>
						<th>Blob Id</th>
						<th>Play button</th>
					</tr>
				</thead>
				<tbody>

					{renderList}

				</tbody>
			</table>
		</div>
	)
}
