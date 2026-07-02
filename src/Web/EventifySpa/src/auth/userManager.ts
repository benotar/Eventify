import {UserManager, WebStorageStateStore} from "oidc-client-ts";

const userManager = new UserManager({
    authority: "https://localhost:5001",
    client_id: "eventify-spa",
    redirect_uri: "https://localhost:5173/callback",
    post_logout_redirect_uri: "https://localhost:5173",
    scope: "openid profile offline_access catalog.read",
    userStore: new WebStorageStateStore({store: window.sessionStorage}),
    automaticSilentRenew: true,
    loadUserInfo: true
});

export default userManager;