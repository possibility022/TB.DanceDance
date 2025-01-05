import React from "react"

interface ILogoutButtonProps {
	singoutRedired(): Promise<void>
}

const LogoutButton = (props: ILogoutButtonProps) => {
	return (
		<button className="button" onClick={() => { props.singoutRedired().catch(e => console.log(e)) }} >
			Log Out
		</button>
	)
}

export default LogoutButton