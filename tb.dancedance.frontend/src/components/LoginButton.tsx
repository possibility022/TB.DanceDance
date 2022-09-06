import React from "react"
import { AuthConsumer } from "../providers/AuthProvider"
import { IAuthService } from "../services/AuthService"

const LoginButton = () => (

	<AuthConsumer>
		{({ isAuthenticated, signinRedirect }: IAuthService) =>
			<button className="button" onClick={() => {
				if (isAuthenticated())
					return
				signinRedirect().catch((e) => console.log(e))
			}}>Log In</button>
		}
	</AuthConsumer>
)

export default LoginButton