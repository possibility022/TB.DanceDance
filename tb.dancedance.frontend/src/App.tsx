import React from "react"
import logo from "./logo.svg"
import LoginButton from "./components/LoginButton"
import LogoutButton from "./components/LogoutButton"
import "./App.css"
import { useAuth0 } from "@auth0/auth0-react"

function App() {

	const { user, isAuthenticated, isLoading } = useAuth0()

	return (
		<section className="section">
			<div className="container">
				<h1 className="title">
					Hello World
				</h1>
				<p className="subtitle">
					My first website with <strong>Bulma</strong>!
				</p>

				<LoginButton></LoginButton>
				<LogoutButton></LogoutButton>

				{
					isAuthenticated && (
						<div>
							<img src={user?.picture} alt={user?.name} />
							<h2>{user?.name}</h2>
							<p>{user?.email}</p>
						</div>

					)}
			</div>
		</section>
	)
}

export default App
