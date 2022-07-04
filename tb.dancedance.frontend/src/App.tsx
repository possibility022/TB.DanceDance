import React, { useEffect, useState } from "react"
import NavigationBar from "./components/NavigationBar"
import "./App.css"
import "bulma/css/bulma.min.css"
import Home from "./pages/Home"

import { AuthProvider } from 'oidc-react'
import { BrowserRouter, Routes, Route } from "react-router-dom"

function App() {

	const [userIsSignedIn, setUserIsSignedIn] = useState(false)

	return (
		<AuthProvider
			authority="https://localhost:7068"
			clientId="tbdancedancefront"
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
			<NavigationBar userIsSignedIn={userIsSignedIn}></NavigationBar>

			<BrowserRouter>
				<Routes>
					<Route path="/" element={<Home />}>
					</Route>
				</Routes>
			</BrowserRouter>


		</AuthProvider>
	)
}

export default App
