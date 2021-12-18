import React, { useEffect, useState } from "react"
import NavigationBar from "./components/NavigationBar"
import "./App.css"
import "bulma/css/bulma.min.css"
import { VideoInfoService } from "./services/VideoInfoService"
import { VideoList } from "./components/Videos/VideoList"
import VideoInformations from "./types/VideoInformations"

function App() {

	const service = new VideoInfoService()

	useEffect(() => {
		// eslint-disable-next-line @typescript-eslint/no-floating-promises
		service.LoadVideos().then((v) => {
			setVideos(v)
		}).catch(r => {
			console.log(r)
		})
	}, []);

	const [videos, setVideos] = useState<Array<VideoInformations>>([]);

	return (
		<div>
			<NavigationBar></NavigationBar>
			<section className="section">
				<div className="container">
					<p className="subtitle">
						Zatańczmy <strong>Razem</strong>!
					</p>

					<VideoList Videos={videos}></VideoList>

				</div>
			</section>
		</div>
	)
}

export default App
