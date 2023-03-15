import { useRef } from "react"
import appClient from "../../services/AppClient"

interface ILoginButtonProps {
	signinRedirect(): Promise<void>
}

const LoginButton = (props: ILoginButtonProps) => {


	const buttonRef = useRef<HTMLButtonElement>(null)

	const loginAction = async () => {
		buttonRef.current?.classList.add('is-loading')
		await appClient.warmupRequest()
		await props.signinRedirect()
	}

	return (
		<button ref={buttonRef} className="button" onClick={() => {
			loginAction()
				.catch(e => console.error(e))
				.finally(() => {
					buttonRef.current?.classList.remove('is-loading')
				})
		}}>Log In</button>
	)
}

export default LoginButton