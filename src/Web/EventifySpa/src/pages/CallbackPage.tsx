import {type FC, useEffect} from "react";
import {useNavigate} from "react-router-dom";
import {useTranslation} from "react-i18next";
import userManager from "../auth/userManager.ts";

const CallbackPage: FC = () => {
    const navigate = useNavigate();
    const {t} = useTranslation();

    useEffect(() => {
        userManager
            .signinRedirectCallback()
            .then(() => navigate("/", {replace: true}))
            .catch(() => navigate("/", {replace: true}))
    }, [navigate]);

    return (
        <div className="flex min-h-screen items-center justify-center">
            <p className="text-fg-muted">{t("common.loading")}</p>
        </div>
    );
};
export default CallbackPage;