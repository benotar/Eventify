import type {User} from "oidc-client-ts";
import {createContext, type FC, type PropsWithChildren, useContext, useEffect, useState} from "react";
import userManager from "./userManager.ts";

interface AuthContextValue {
    user: User | null;
    isLoading: boolean;
    isAuthenticated: boolean;
    login: () => Promise<void>;
    logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

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

    const value: AuthContextValue = {
        user,
        isLoading,
        isAuthenticated: user !== null && !user.expired,
        login: () => userManager.signinRedirect(),
        logout: () => userManager.signoutRedirect(),
    };

    return (
        <AuthContext.Provider value={value}>
            {children}
        </AuthContext.Provider>
    );
};

export const useAuth = (): AuthContextValue => {
    const context = useContext(AuthContext);

    if (context === undefined) {
        throw new Error("useAuth must be used within an AuthProvider");
    }

    return context;
};

export default AuthProvider;