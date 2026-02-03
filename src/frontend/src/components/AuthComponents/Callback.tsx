import { useContext, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { AuthContext } from "../../providers/AuthProvider";

export const Callback = () => {

    const navigate = useNavigate()
    const authContext = useContext(AuthContext)
    const [errorMessage, setErrorMessage] = useState('')
    const [isProcessing, setIsProcessing] = useState(true)

    useEffect(() => {
        authContext.signinRedirectCallback()
            .then(() => {
                setIsProcessing(false)
                if (authContext.isAuthenticated()) {
                    navigate('/');
                }
            })
            .catch((e) => {
                setIsProcessing(false)
                if (e['error'] === 'login_required') {
                    authContext.signinRedirect()
                        .catch(console.error)
                } else {
                    console.error(e)
                    setErrorMessage(
                        'Smuteczek :(. Z jakiegoś powodu nie udało się zalogować. Spróbuj jeszcze raz.'
                    );
                }
            });
    }, [authContext, navigate]);

    useEffect(() => {
        if (!isProcessing && !errorMessage) {
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
        }
    }, [isProcessing, errorMessage, authContext, navigate]);

    return (
        <div>
            {isProcessing && <p>Za moment zostaniesz przekierowany :)</p>}
            {errorMessage && <p>{errorMessage}</p>}
        </div>
    )
}