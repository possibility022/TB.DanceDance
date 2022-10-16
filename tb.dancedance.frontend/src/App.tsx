import React, { useState } from "react"
import NavigationBar from "./components/NavigationBar"
import "./App.css"
import "bulma/css/bulma.min.css"
import Home from "./pages/Home"
import { PrivateScreen } from "./pages/PrivateScreen"

import { BrowserRouter, Routes, Route, NavLink, Link } from "react-router-dom"
import { Callback } from "./components/AuthComponents/Callback"
import { Logout } from "./components/AuthComponents/Logout"
import { LogoutCallback } from "./components/AuthComponents/LogoutCallback"
import { PrivateRoute } from "./components/AuthComponents/PrivateRoute"
import { SilentRenew } from "./components/AuthComponents/SilentRenew"
import { AuthProvider } from "./providers/AuthProvider"
import { VideoScreen } from "./pages/VideosScreen"
import { VideoList } from "./components/Videos/VideoList"
import { VideoPlayerScreen } from "./pages/VideoPlayerScreen"
import { LoginLogout } from "./components/LoginLogoutComponents/LoginLogout"

function App() {

	return (
		<div className="container">
			<AuthProvider>

				<BrowserRouter>
				<NavigationBar></NavigationBar>
					<Routes>
						<Route path="/" element={<Home />}>
						</Route>
						<Route path="videos/" element={<PrivateRoute element={<VideoScreen></VideoScreen>} />}>
						</Route>
						<Route path="videos/:videoId" element={<PrivateRoute element={<VideoPlayerScreen></VideoPlayerScreen>} />} />
						<Route path="callback" element={<Callback />} />
						<Route path="logout" element={<Logout></Logout>} />
						<Route path="logout/callback" element={<LogoutCallback />} />
						{/* <Route path="/register" element={Register} /> */}
						<Route path="silentrenew" element={<SilentRenew />} />
						{/* <Route path="/" element={PublicPage} /> */}

					</Routes>
				</BrowserRouter>
			</AuthProvider>
		</div>
	)
}

export default App
