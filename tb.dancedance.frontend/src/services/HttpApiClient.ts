import axios, { AxiosInstance } from 'axios';
import { AuthService, TokenProvider } from './AuthService';


const tokenProvider: TokenProvider = new AuthService()

export const apiClientFactory = () => {
    const instance = axios.create({
        baseURL: 'https://localhost:7068'
    });

    applyInterceptor(instance)

    return instance
}

const applyInterceptor = (axiosInstance: AxiosInstance) => {
    axiosInstance.interceptors.request.use(c => {

        let config = c

        if (config === undefined)
            config = {}

        if (c.headers && c.headers['Authorization']) {
            // authorization header is already set, nothing to do
        } else {
            const token = tokenProvider.getAccessToken()
            if (token) {
                const fullToken = `Bearer ${token}`
                if (c.headers) {
                    c.headers['Authorization'] = fullToken
                } else {
                    c.headers = {
                        'Authorization': fullToken
                    }
                }
            }
        }

        return c
    })
}