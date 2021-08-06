import { useState } from "react"
import React from "react"
import { useDispatch, useSelector } from "react-redux"
import { LogIn } from "../actions/VideosActions"

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export default function Login() {
	const [email, setEmail] = useState("")
	const [password, setPassword] = useState("")

	const dispatch = useDispatch()
	const LogInAction = () => { dispatch(LogIn(email, password))}

	function validateForm() {
		return email.length > 0 && password.length > 0
	}

	function handleSubmit(event: { preventDefault: () => void }) {
		event.preventDefault()
		LogInAction()
	}

	return (
		<div className="container">
			<h1>Zaloguj się obca mi osobo :)</h1>
			<form className="form" onSubmit={handleSubmit}>
				<div>
					<label className="form-group">
						<input
							className="form-control"
							placeholder="email@tutaj.com"
							autoFocus
							type="email"
							value={email}
							onChange={(e) => setEmail(e.target.value)}
						/>
						<span className="form-label">Email</span>
					</label>
				</div>
				<div>
					<label className="form-group">
						<span className="form-label">Password</span>
						<input
							className="form-control"
							type="password"
							placeholder="haseło tutaj"
							value={password}
							onChange={(e) => setPassword(e.target.value)}
						/>
					</label>
				</div>
				<button disabled={!validateForm()}>
            Zatańczmy (login)
				</button>
			</form>
		</div>
	)
}