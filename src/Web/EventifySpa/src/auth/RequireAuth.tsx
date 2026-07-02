import {type FC, useEffect, useRef} from "react";
import {useAuth} from "./useAuth.ts";
import Loading from "../components/Loading.tsx";
import {Outlet} from "react-router-dom";

const RequireAuth: FC = () => {
    const {isLoading, isAuthenticated, login} = useAuth();

    const triggered = useRef(false);

    useEffect(() => {
        if (!isLoading && !isAuthenticated && !triggered.current) {
            triggered.current = true;
            login().catch(console.error);
        }
    }, [isLoading, isAuthenticated, login]);

    if (isLoading || !isAuthenticated) {
        return <Loading fullscreen/>;
    }

    return <Outlet/>;
};

export default RequireAuth;