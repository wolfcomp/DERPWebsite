import { useContext, createContext, useState, useEffect } from "react";
import { RequestError, useRequest } from "./request";
import { deleteCookie, getCookie, setCookie } from "./cookie";

export const AuthContext = createContext<{
    user: {
        name: string;
        id: string;
        avatar: string;
        role: string;
        token: string;
    } | null;
    login: (uid: string) => Promise<void>;
    logout: () => Promise<void>;
} | null>(null);

export function useAuth() {
    return useContext(AuthContext);
}

export function AuthProvider(props: any) {
    const [user, setUser] = useState<{
        name: string;
        id: string;
        avatar: string;
        role: string;
        token: string;
    } | null>(null);
    const requestContext = useRequest();
    const request = requestContext.request;

    async function login(uid: string) {
        var resp = await request("/api/auth/login?userId=" + uid, {
            method: "GET"
        });
        setUser(await resp.json());
    }

    async function logout() {
        var resp = await request("/api/auth/logout?token=" + user.token, {
            method: "DELETE"
        });
        setUser(null);
    }

    // async function check() {
    //     var resp = await request("/api/auth/me", {
    //         method: "GET",
    //     });
    //     if (resp.status === 200) {
    //         setUser(await resp.json());
    //     }
    //     else {
    //         setUser(null);
    //     }
    // }

    async function refresh() {
        var token = getCookie("token")?.value;
        if (!token && user) {
            token = user.token;
        }
        if (!token) {
            setUser(null);
            return;
        }
        var resp: Response;
        try {
            resp = await request("/api/auth/refresh", {
                method: "POST",
                headers: {
                    "Authorization": "Bearer " + token,
                }
            });
            if (resp.status === 200) {
                setUser(await resp.json());
            }
            else {
                setUser(null);
            }
        }
        catch (e) {
            setUser(null);
        }
    }

    useEffect(() => {
        refresh();
    }, [setUser]);

    useEffect(() => {
        requestContext.setAuth(user);
        if (user) {
            setCookie("token", user.token, 7);
        }
        else {
            deleteCookie("token");
        }
    }, [user]);

    return (
        <AuthContext.Provider value={{
            user: user,
            login,
            logout
        }}>
            {props.children}
        </AuthContext.Provider>
    );
}