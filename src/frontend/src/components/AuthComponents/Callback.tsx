import { useCallback, useContext, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { AuthContext } from "../../providers/AuthProvider";

export const Callback = () => {

    const navigate = useNavigate()

    const authContext = useContext(AuthContext)

    const callback = useCallback(() => {
        authContext.signinRedirectCallback()
            .catch((e) => {
                if (e['error'] === 'login_required') {
                    authContext.signinRedirect()
                        .catch(console.error)
                }
                console.log(e)
                void navigate('/') // todo navigate to returned url
            },)

        return "Za moment zostaniesz przekierowany :)"
    }, [])

    const [errorMessage, setErrorMessage] = useState('')

    useEffect(() => {
        const timeoutId = setTimeout(() => {
            if (authContext.isAuthenticated()) {
                navigate('/');
            } else {
                setErrorMessage(
                    'Smuteczek :(. Z jakiegoś powodu nie udało się zalogować. Spróbuj jeszcze raz.'
                );
            }
        }, 3000);

        return () => clearTimeout(timeoutId);
    }, [authContext, navigate]);


    // dont look at that :| I don't know how to do it in proper way at this moment
    return (<div>
        {callback()}
        {errorMessage}
    </div>)


}