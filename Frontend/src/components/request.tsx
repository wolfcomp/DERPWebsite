import { useContext, createContext, useState } from "react";
import Loader from "./loader";

const RequestContext = createContext<{
    setAuth: React.Dispatch<React.SetStateAction<{
        name: string;
        id: string;
        avatar: string;
        roleName: string;
        roleId: string;
        token: string;
    } | null>>;
    request: (url: string, options?: RequestInit) => Promise<Response>;
}>({
    setAuth: () => { throw new Error("Request provider not initialized") },
    request: async () => { throw new Error("Request provider not initialized") }
});

export class RequestError extends Error {
    constructor(public response: Response) {
        super(response.statusText);
    }
}

export function useRequest() {
    return useContext(RequestContext);
}

export default function RequestProvider({ children }: { children: React.ReactNode }) {
    const [auth, setAuth] = useState<{
        name: string;
        id: string;
        avatar: string;
        roleName: string;
        roleId: string;
        token: string;
    } | null>(null);
    const [requesting, setRequesting] = useState<boolean>(false);
    const [isAuthing, setIsAuthing] = useState<boolean>(false);

    async function request(url: string, options: RequestInit = {}): Promise<Response> {
        if (!!auth)
            options = {
                ...options,
                headers: {
                    ...options.headers,
                    "Authorization": "Bearer " + auth.token
                }
            }
        if (process.env.REACT_APP_API_URL) {
            url = process.env.REACT_APP_API_URL + url;
        }
        if (url.includes("auth") && !isAuthing) {
            setIsAuthing(true);
        }
        setRequesting(true);
        var response: Response;
        try {
            response = await fetch(url, options);
            if (!response.ok) {
                if (process.env.NODE_ENV === "development") {
                    console.error(response);
                    console.error(await response.text());
                }
                throw new RequestError(response);
            }
            return response;
        }
        finally {
            setRequesting(false);
        }
    }

    return (
        <RequestContext.Provider value={{ request, setAuth }} >
            {children}
            {requesting && <Loader />}
        </RequestContext.Provider>
    );
}