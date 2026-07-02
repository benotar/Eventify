import {type FC, useEffect} from "react";
import {useNavigate} from "react-router-dom";
import userManager from "../auth/userManager.ts";
import Loading from "../components/Loading.tsx";

const CallbackPage: FC = () => {
    const navigate = useNavigate();

    useEffect(() => {
        userManager
            .signinRedirectCallback()
            .then(() => navigate("/", {replace: true}))
            .catch(() => navigate("/", {replace: true}))
    }, [navigate]);

    return <Loading fullscreen/>
};
export default CallbackPage;