import React from "react"

interface ILoginButtonProps {
	signinRedirect(): Promise<void>
}

const LoginButton = (props: ILoginButtonProps) =>
(
	<button className="button" onClick={() => { props.signinRedirect().catch(e => console.error(e)) }}>Log In</button>
)

export default LoginButton