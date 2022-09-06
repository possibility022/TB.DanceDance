import React, { useEffect, useState } from "react"
import NavigationBar from "./components/NavigationBar"
import "./App.css"
import "bulma/css/bulma.min.css"
import Home from "./pages/Home"

import { BrowserRouter, Routes, Route, useNavigate } from "react-router-dom"
import { Callback } from "./components/AuthComponents/Callback"
import { Logout } from "./components/AuthComponents/Logout"
import { LogoutCallback } from "./components/AuthComponents/LogoutCallback"
import { PrivateRoute } from "./components/AuthComponents/PrivateRoute"
import { SilentRenew } from "./components/AuthComponents/SilentRenew"
import { AuthProvider } from "./providers/AuthProvider"

function App() {


	const [userIsSignedIn, setUserIsSignedIn] = useState(false)

	return (
		<div>
			<NavigationBar userIsSignedIn={userIsSignedIn}></NavigationBar>

			<AuthProvider>
				<BrowserRouter>
					<Routes>
						<Route path="/" element={<Home />}>
						</Route>

						<Route path="/private" element={<PrivateRoute element={<Home></Home>} />} />

						<Route path="/callback" element={<Callback />} />
						<Route path="/logout" element={<Logout></Logout>} />
						<Route path="/logout/callback" element={<LogoutCallback/>} />
						{/* <Route path="/register" element={Register} /> */}
						<Route path="/silentrenew" element={<SilentRenew/>} />
						{/* <Route path="/" element={PublicPage} /> */}

					</Routes>
				</BrowserRouter>
			</AuthProvider>
		</div>
	)
}

export default App
