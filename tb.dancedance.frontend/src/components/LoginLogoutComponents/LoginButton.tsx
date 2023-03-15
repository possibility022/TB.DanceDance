import appClient from "../../services/AppClient"

interface ILoginButtonProps {
	signinRedirect(): Promise<void>
}

const LoginButton = (props: ILoginButtonProps) => {

	const loginAction = async () => {
		await appClient.warmupRequest()
		await props.signinRedirect()
	}

	return (
		<button className="button" onClick={() => {
			loginAction().catch(e => console.error(e))
		}}>Log In</button>
	)
}

export default LoginButton