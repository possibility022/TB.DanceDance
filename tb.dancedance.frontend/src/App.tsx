import React, { useEffect, useState } from "react"
import NavigationBar from "./components/NavigationBar"
import "./App.css"
import "bulma/css/bulma.min.css"
import { VideoInfoService, Video } from "./services/VideoInfoService"

function App() {

	const service = new VideoInfoService()
	const [videos, setVideos] = useState<[Video]>();

	useEffect(() => {
		// eslint-disable-next-line @typescript-eslint/no-floating-promises
		service.LoadVideos().then((v) => {
			setVideos(v)

			const s = v.map(r => {
				return <li key={r.id}>{r.id} - {r.name}</li>
			})

			setVideoList(s)
		}).catch(r => {
			console.log(r)
		})
	}, []);

	const [videoList, setVideoList] = useState<JSX.Element[]>();

	return (
		<div>
			<NavigationBar></NavigationBar>
			<section className="section">
				<div className="container">
					<h1 className="title">
						Hello World
					</h1>
					<p className="subtitle">
						My first website with <strong>Bulma</strong>!
					</p>

					{videoList}

				</div>
			</section>
		</div>
	)
}

export default App
