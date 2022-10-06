import axios from 'axios';
import { AuthService, TokenProvider } from './AuthService';

const apiClient = axios.create({
    baseURL: 'https://localhost:7068'
});

const tokenProvider: TokenProvider = new AuthService()

apiClient.interceptors.request.use(c => {

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

export default apiClient;