# OIDC Authorization Code + PKCE — як це працює в Eventify

## Загальна картина

Identity Server (IS) — окремий мікросервіс на `https://localhost:5001`.
Його єдина відповідальність: **видавати токени** після перевірки особи користувача.
SPA і всі API-сервіси довіряють тільки токенам від IS.

Протоколи: **OAuth 2.0** (авторизація) + **OpenID Connect / OIDC** (автентифікація поверх OAuth).

---

## Authorization Code + PKCE — покроковий флов

```
SPA                          IS                        Браузер
 |                            |                            |
 | 1. login()                 |                            |
 |   генерує code_verifier    |                            |
 |   рахує code_challenge     |                            |
 |   redirect на IS --------->|                            |
 |                            | 2. показує форму логіну   |
 |                            |   (Razor Page) ---------->|
 |                            |                            |
 |                            |<-- 3. POST email + пароль-|
 |                            |                            |
 |<-- 4. redirect /callback?code=XXX&state=YYY ------------|
 |                            |                            |
 | 5. signinRedirectCallback()|                            |
 |   POST /connect/token      |                            |
 |   { code, code_verifier } >|                            |
 |<-- 6. access_token         |                            |
 |       id_token             |                            |
 |       refresh_token        |                            |
 |                            |                            |
 | 7. зберегти в sessionStorage                            |
 | 8. navigate("/")           |                            |
```

---

## Що таке PKCE і навіщо

**Проблема:** SPA — публічний клієнт. Його JS-код читабельний у браузері,
тому він **не може** зберігати `client_secret` (будь-хто міг би його вкрасти).

**PKCE** (Proof Key for Code Exchange) — вирішення без секрету:

```
1. SPA генерує  code_verifier  — випадковий рядок 43–128 символів
2. Рахує        code_challenge = base64url( SHA256(code_verifier) )
3. Відправляє   code_challenge  з першим запитом (відкрито, не секрет)
4. IS запам'ятовує challenge, видає code
5. SPA відправляє code + оригінальний code_verifier при обміні на токени
6. IS перевіряє: SHA256(verifier) == challenge → впевнений що той самий клієнт
```

Навіть якщо зловмисник перехопить `code` — без `code_verifier` він марний.

---

## Токени

| Токен | Що містить | Живе | Хто використовує |
|---|---|---|---|
| `id_token` | JWT: хто ти (sub, email, name) | ~5 хв | SPA — читає профіль |
| `access_token` | JWT: що можеш робити (scopes) | ~1 год | SPA → надсилає до API |
| `refresh_token` | непрозорий рядок | дні/тижні | oidc-client-ts тихо оновлює access_token |

**sub** — Subject Identifier: незмінний унікальний ID користувача в IS (UUID).

---

## Конфігурація клієнта в SeedData.cs

```csharp
new Client
{
    ClientId = "eventify-spa",           // SPA передає це в кожному запиті
    AllowedGrantTypes = GrantTypes.Code, // тільки Authorization Code flow
    RequirePkce = true,                  // PKCE обов'язковий
    RequireClientSecret = false,         // SPA не може зберігати секрет — це ок
    RedirectUris = { "https://localhost:5173/callback" },      // куди після логіну
    PostLogoutRedirectUris = { "https://localhost:5173" },     // куди після логауту
    AllowedCorsOrigins = { "https://localhost:5173" },         // CORS: токен endpoint
    AllowedScopes = { "openid", "profile", "catalog.read" },
    AllowOfflineAccess = true            // дозволяє refresh_token
}
```

`AllowedCorsOrigins` — критично для SPA: крок 5 (обмін code → токени) — це
HTTP POST з браузера до IS. Без CORS браузер заблокує відповідь.

---

## Конфігурація в SPA — userManager.ts

```ts
const userManager = new UserManager({
    authority: "https://localhost:5001",              // IS — discovery endpoint
    client_id: "eventify-spa",                        // збігається з SeedData
    redirect_uri: "https://localhost:5173/callback",  // збігається з RedirectUris
    scope: "openid profile",
    userStore: new WebStorageStateStore({ store: window.sessionStorage })
})
```

При першому виклику `oidc-client-ts` завантажує
`https://localhost:5001/.well-known/openid-configuration` — **discovery document**.
Це JSON з усіма endpoint-ами IS (authorize, token, userinfo, etc.).
Бібліотека читає їх звідти автоматично — не треба прописувати вручну.

---

## Де що зберігається

```
sessionStorage["oidc.user:https://localhost:5001:eventify-spa"]
  → серіалізований User об'єкт з токенами
```

`sessionStorage` — очищається при закритті вкладки (не браузера).
Обрано свідомо: якщо вкладка закрита — сесія починається заново.
`localStorage` зберігав би токени між сесіями — більший ризик XSS.

---

## AuthProvider.tsx — як React бачить стан

```ts
userManager.getUser()                    // читає з sessionStorage при mount
  .then(setUser)                         // null якщо не авторизований
  .finally(() => setIsLoading(false))    // знімає loading стан

userManager.events.addUserLoaded(...)    // спрацьовує при тихому оновленні токену
userManager.events.addUserUnloaded(...) // спрацьовує при logout
```

`isAuthenticated = user !== null && !user.expired`
— перевіряє і наявність, і строк дії токену.

---

## CallbackPage.tsx — чому він важливий

Після логіну IS робить redirect на `/callback?code=XXX&state=YYY&session_state=ZZZ`.
Цей URL треба **обробити** — просто показати сторінку недостатньо.

```ts
userManager.signinRedirectCallback()
  // читає code і state з window.location
  // POST /connect/token { code, code_verifier, client_id, redirect_uri }
  // отримує токени, зберігає в sessionStorage
  .then(() => navigate("/", { replace: true }))
```

`replace: true` — замінює `/callback` в history браузера.
Кнопка "назад" не повертає на порожню callback-сторінку.

---

## Logout

```ts
userManager.signoutRedirect()
  // redirect на IS /connect/endsession
  // IS очищає свою сесію
  // redirect назад на PostLogoutRedirectUri = "https://localhost:5173"
```

IS сесія і SPA сесія — різні речі. Logout очищає обидві.

---

## Discovery Document — що це

`https://localhost:5001/.well-known/openid-configuration`

Публічний JSON який IS видає автоматично. Містить всі endpoint-и:

```json
{
  "issuer": "https://localhost:5001",
  "authorization_endpoint": "https://localhost:5001/connect/authorize",
  "token_endpoint": "https://localhost:5001/connect/token",
  "userinfo_endpoint": "https://localhost:5001/connect/userinfo",
  "jwks_uri": "https://localhost:5001/.well-known/openid-configuration/jwks",
  ...
}
```

`jwks_uri` — публічні ключі IS для перевірки підпису JWT токенів.
API-сервіси (Catalog, Booking) завантажують ці ключі і перевіряють
підпис кожного `access_token` без звернення до IS.

---

## Seeder — чому клієнти в БД, а не в коді

`IdentityServerSeeder` (IHostedService) запускається при старті IS.
Якщо таблиця клієнтів порожня — вставляє з `SeedData.cs`.

Клієнти зберігаються в **Postgres** (не in-memory).
Це дозволяє IS горизонтально масштабуватись — кілька інстансів читають
з одного джерела. In-memory конфіг не підходить для production.

**Важливо:** seeder спрацьовує **лише один раз** (перевірка `AnyAsync`).
Якщо треба змінити конфіг клієнта — або вручну оновити БД,
або видалити рядки і перезапустити IS.
