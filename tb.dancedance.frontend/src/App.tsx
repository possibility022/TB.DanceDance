import React, { useState } from "react"
import NavigationBar from "./components/NavigationBar"
import "./App.css"
import "bulma/css/bulma.min.css"
import Home from "./pages/Home"
import { PrivateScreen } from "./pages/PrivateScreen"

import { BrowserRouter, Routes, Route } from "react-router-dom"
import { Callback } from "./components/AuthComponents/Callback"
import { Logout } from "./components/AuthComponents/Logout"
import { LogoutCallback } from "./components/AuthComponents/LogoutCallback"
import { PrivateRoute } from "./components/AuthComponents/PrivateRoute"
import { SilentRenew } from "./components/AuthComponents/SilentRenew"
import { AuthProvider } from "./providers/AuthProvider"

function App() {

	return (
		<div>
			<AuthProvider>

				<NavigationBar></NavigationBar>
				<BrowserRouter>
					<Routes>
						<Route path="/" element={<Home />}>
						</Route>

						<Route path="/private" element={<PrivateRoute element={<PrivateScreen videoUrl="https://localhost:7068/api/video/stream/8aea6fac-a1cb-420d-bffa-ffc56e4c43f0"></PrivateScreen>} />} />
						<Route path="/callback" element={<Callback />} />
						<Route path="/logout" element={<Logout></Logout>} />
						<Route path="/logout/callback" element={<LogoutCallback />} />
						{/* <Route path="/register" element={Register} /> */}
						<Route path="/silentrenew" element={<SilentRenew />} />
						{/* <Route path="/" element={PublicPage} /> */}

					</Routes>
				</BrowserRouter>
			</AuthProvider>
		</div>
	)
}

export default App
