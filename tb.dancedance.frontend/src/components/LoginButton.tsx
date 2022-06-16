import { useAuth } from "oidc-react";
import React from "react"

const LoginButton = () => {

	const auth = useAuth();

	return <button className="button" onClick={() => auth.signIn()}>Log In</button>
}

export default LoginButton