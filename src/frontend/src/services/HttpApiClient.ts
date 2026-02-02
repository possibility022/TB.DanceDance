import axios, {AxiosInstance} from 'axios';
import { authService } from '../providers/AuthProvider';
import { TokenProvider } from './AuthService';


const tokenProvider: TokenProvider = authService

const apiClientFactory = () => {
    const instance = axios.create({
        baseURL: process.env.REACT_APP_API_BASE_URL
    });

    applyInterceptor(instance)

    return instance
}

const applyInterceptor = (axiosInstance: AxiosInstance) => {
    axiosInstance.interceptors.request.use(async c => {

        if (c.headers && c.headers['Authorization']) {
            // authorization header is already set, nothing to do
        } else {
            const token = await tokenProvider.getAccessToken()
            if (token) {
                const fullToken = `Bearer ${token}`
                if (c.headers) {
                    c.headers['Authorization'] = fullToken
                }
            }
        }

        return c
    })
}

// singleton instance
const AppApiClient = apiClientFactory()
export default AppApiClient