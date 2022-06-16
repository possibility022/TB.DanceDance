import { useAuth } from "oidc-react"
import React from "react"

const LogoutButton = () => {
	const auth = useAuth()

	return (
		<button className="button" onClick={() => auth.signOut()}>
      Log Out
		</button>
	)
}

export default LogoutButton