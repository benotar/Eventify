import type {FC} from "react";
import {useTranslation} from "react-i18next";

const LANGUAGES = ["en", "uk"] as const;

const LanguageSwitcher: FC = () => {
    const {i18n} = useTranslation();

    const changeLanguage = (lang: string) => {
        i18n.changeLanguage(lang);
        localStorage.setItem("lang", lang);
    };

    return (
        <div className="flex items-center rounded-full border border-border bg-surface-2 p-1 backdrop-blur-md">
            {LANGUAGES.map((lang) => (
                <button
                    key={lang}
                    onClick={() => changeLanguage(lang)}
                    className={`cursor-pointer rounded-full px-4 py-1.5 text-xs font-bold uppercase tracking-widest transition-colors
  focus:outline-none focus:ring-2 focus:ring-brand ${
                        i18n.language.startsWith(lang)
                            ? "bg-fg text-bg"
                            : "text-fg-muted hover:text-fg"
                    }`}
                >
                    {lang}
                </button>
            ))}
        </div>
    );
};

export default LanguageSwitcher;