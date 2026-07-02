import type {FC} from "react";
import {useTranslation} from "react-i18next";
import {Loader} from "lucide-react";

interface LoadingProps {
    label?: string;
    fullscreen?: boolean
}

const Loading: FC<LoadingProps> = ({label, fullscreen}) => {
    const {t} = useTranslation();

    const text = label ?? t("common.loading");

    const fullScreenStyle = fullscreen
        ? "min-h-[calc(100vh-4rem)]"
        : "py-12";

    return (
        <div className={`flex items-center justify-center gap-3 ${fullScreenStyle}`}>
            <Loader className="animate-spin text-brand" size={20}/>
            <span className="text-sm text-fg-muted">{text}</span>
        </div>
    );
};

export default Loading;