import {type FC, type PropsWithChildren, useCallback, useEffect, useMemo, useState} from "react";
import type {User} from "oidc-client-ts";
import userManager from "./userManager.ts";
import {AuthContext, type AuthContextValue} from "./useAuth.ts";

const AuthProvider: FC<PropsWithChildren> = ({children}) => {
    const [user, setUser] = useState<User | null>(null);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        userManager
            .getUser()
            .then(setUser)
            .finally(() => setIsLoading(false));

        const onLoaded = (loaded: User) => setUser(loaded);
        const onUnloaded = () => setUser(null);

        userManager.events.addUserLoaded(onLoaded);
        userManager.events.addUserUnloaded(onUnloaded);

        return () => {
            userManager.events.removeUserLoaded(onLoaded);
            userManager.events.removeUserUnloaded(onUnloaded);
        };
    }, []);

    const login = useCallback(() => userManager.signinRedirect(), []);
    const logout = useCallback(() => userManager.signoutRedirect(), []);

    const value = useMemo<AuthContextValue>(() => ({
        user,
        isLoading,
        isAuthenticated: user !== null && !user.expired,
        login,
        logout
    }), [user, isLoading, login, logout]);

    return (
        <AuthContext.Provider value={value}>
            {children}
        </AuthContext.Provider>
    );
};

export default AuthProvider;