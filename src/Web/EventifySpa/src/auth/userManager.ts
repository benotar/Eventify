import {UserManager, WebStorageStateStore} from "oidc-client-ts";

const userManager = new UserManager({
    authority: "https://localhost:5001",
    client_id: "eventify-spa",
    redirect_uri: "https://localhost:5173/callback",
    scope: "openid profile",
    userStore: new WebStorageStateStore({store: window.sessionStorage})
});

export default userManager;