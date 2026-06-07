import i18n from "i18next";
import {initReactI18next} from "react-i18next";
import enCommon from "./locales/en/common.json";
import ukCommon from "./locales/uk/common.json";

i18n.use(initReactI18next)
    .init({
        resources: {
            en: {common: enCommon},
            uk: {common: ukCommon}
        },
        lng: localStorage.getItem("lang") ?? "en",
        fallbackLng: "en",
        ns: ["common"],
        defaultNS: "common",
        interpolation: {
            escapeValue: false
        }
    });

export default i18n;