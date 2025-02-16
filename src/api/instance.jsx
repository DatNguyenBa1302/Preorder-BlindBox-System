import axios from "axios";
import { GetAccessToken } from "./User/ApiAuthentication";

const api = axios.create({
   baseURL: 'https://localhost:7037/api'
})

api.interceptors.request.use(
   (config) => {
      const accessToken = GetAccessToken();
      if (accessToken) {
         config.headers.Authorization = `Bearer ${accessToken}`;
      }
      return config
   },
   (error) => {
      return Promise.reject(error);
   }
)

export default api