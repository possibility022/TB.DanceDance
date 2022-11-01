import { AuthConsumer } from "../../providers/AuthProvider";
import { IAuthService } from "../../services/AuthService";

export const Callback = () => {
    return <AuthConsumer>
        {({ signinRedirectCallback }: IAuthService) => {
            signinRedirectCallback()
                .then(() => {
                    // todo: check why redirect urls stays in URL address
                })
                .catch(e => console.error(e));
            return <span>:D</span>;
        }}
    </AuthConsumer>

}