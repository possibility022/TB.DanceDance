import React from "react";
import { AuthConsumer } from "../../providers/AuthProvider";
import { IAuthService } from "../../services/AuthService";
import { useNavigate } from "react-router-dom";



export const Callback = () => {
    const navigate = useNavigate();
    return <AuthConsumer>
        {({ signinRedirectCallback }: IAuthService) => {
            signinRedirectCallback()
                .then(() => {
                    navigate('/private')
                })
                .catch(e => console.error(e));
            return <span>:D</span>;
        }}
    </AuthConsumer>

}