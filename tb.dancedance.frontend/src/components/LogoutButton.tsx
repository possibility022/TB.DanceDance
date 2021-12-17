import React from "react"
import { useAuth0 } from "@auth0/auth0-react"
import "bulma/css/bulma.min.css"
import { Button } from "react-bulma-components"

const LogoutButton = () => {
	const { logout } = useAuth0()

	return (
		<Button onClick={() => logout({ returnTo: window.location.origin })}>
      Log Out
		</Button>
	)
}

export default LogoutButton