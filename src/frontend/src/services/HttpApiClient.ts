import axios, {AxiosInstance} from 'axios';
import { authService } from '../providers/AuthProvider';
import { TokenProvider } from './AuthService';
import ConfigProvider from "./ConfigProvider";


const tokenProvider: TokenProvider = authService

const apiClientFactory = () => {
    const instance = axios.create({
        baseURL: ConfigProvider.resolveApiHost()
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