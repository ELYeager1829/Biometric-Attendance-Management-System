import axios from "axios";

const API = axios.create({
  baseURL: "http://localhost:5160/api",
});

export const login = async (data) => {
  const response = await API.post("/Auth/login", data);
  return response.data;
};

export const register = async (data) => {
  const response = await API.post("/Auth/register", data);
  return response.data;
};