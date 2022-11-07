import { useNavigate } from "react-router-dom";
import { AuthConsumer } from "../../providers/AuthProvider";
import { IAuthService } from "../../services/AuthService";

export const Callback = () => {

    const navigate = useNavigate()

    return <AuthConsumer>
        {({ signinRedirectCallback }: IAuthService) => {
            signinRedirectCallback()
                .then(() => {
                    navigate('/')
                    // todo: check why redirect urls stays in URL address
                })
                .catch(e => console.error(e));
            return <span>:D</span>;
        }}
    </AuthConsumer>

}