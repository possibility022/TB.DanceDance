import axios, { AxiosInstance } from 'axios';
import { authService } from '../providers/AuthProvider';
import { TokenProvider } from './AuthService';


const tokenProvider: TokenProvider = authService

const apiClientFactory = () => {

    let baseUrl = process.env.REACT_APP_API_BASE_URL

    if (!baseUrl)
    {
        baseUrl = 'https://localhost:7068'
    }

    const instance = axios.create({
        baseURL: baseUrl
    });

    applyInterceptor(instance)

    return instance
}

const applyInterceptor = (axiosInstance: AxiosInstance) => {
    axiosInstance.interceptors.request.use(async c => {

        let config = c

        if (config === undefined)
            config = {}

        if (c.headers && c.headers['Authorization']) {
            // authorization header is already set, nothing to do
        } else {
            const token = await tokenProvider.getAccessToken()
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

// singleton instance
const AppApiClient = apiClientFactory()
export default AppApiClient