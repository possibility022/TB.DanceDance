import React, { useEffect, useState } from "react"
import NavigationBar from "./components/NavigationBar"
import "./App.css"
import "bulma/css/bulma.min.css"
import { VideoInfoService } from "./services/VideoInfoService"
import { VideoList } from "./components/Videos/VideoList"
import VideoInformations from "./types/VideoInformations"
import { AuthProvider } from 'oidc-react'

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

	const [userIsSignedIn, setUserIsSignedIn] = useState(false)

	return (
		<AuthProvider
			authority="https://localhost:7068"
			clientId="tbdancedancefront"
			responseType
			autoSignIn={true}
			onSignIn={(user) => {
				setUserIsSignedIn(true)
			}}
			onSignOut={(options) => {
				setUserIsSignedIn(false)
			}}
			scope="openid profile tbdancedanceapi.read"
			redirectUri="http://localhost:3000/"
		>

			<div>
				{/* <button onClick={login}>Login</button>
				<button id="api">Call API</button>
				<button id="logout">Logout</button> */}

				<pre id="results"></pre>
			</div>

			<NavigationBar userIsSignedIn={userIsSignedIn}></NavigationBar>
			<section className="section">
				<div className="container">
					<p className="subtitle">
						Zatańczmy <strong>Razem</strong>!
					</p>

					<VideoList Videos={videos}></VideoList>

				</div>
			</section>
		</AuthProvider>
	)
}

export default App
