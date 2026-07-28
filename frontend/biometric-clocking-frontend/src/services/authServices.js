import axios from "axios";

console.log(import.meta.env.VITE_API_URL); 

const API = axios.create({
    baseURL: "http://localhost:5160/api",
    // baseURL: import.meta.env.VITE_API_URL,
    headers: {
        "Content-Type": "application/json",
    },
});

export const login = async (loginData) => {
    const response = await API.post("/Auth/login", loginData);
    return response.data;
};

export const register = async (registerData) => {
    const response = await API.post("/Auth/register", registerData);
    return response.data;
};

export default API;