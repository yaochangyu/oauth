import { UserManager, WebStorageStateStore, type User } from 'oidc-client-ts'

const AUTHORITY = 'https://localhost:7098'
const CLIENT_ID = 'spa-client'
const REDIRECT_URI = 'http://localhost:5173/callback'
const POST_LOGOUT_URI = 'http://localhost:5173'

export const userManager = new UserManager({
  authority: AUTHORITY,
  client_id: CLIENT_ID,
  redirect_uri: REDIRECT_URI,
  post_logout_redirect_uri: POST_LOGOUT_URI,
  response_type: 'code',
  scope: 'openid profile email offline_access api',
  userStore: new WebStorageStateStore({ store: window.localStorage }),
  automaticSilentRenew: false,
})

export async function login(): Promise<void> {
  await userManager.signinRedirect()
}

export async function handleCallback(): Promise<User> {
  return userManager.signinRedirectCallback()
}

export async function logout(): Promise<void> {
  await userManager.signoutRedirect()
}

export async function getUser(): Promise<User | null> {
  return userManager.getUser()
}

export async function isLoggedIn(): Promise<boolean> {
  const user = await userManager.getUser()
  return !!user && !user.expired
}
