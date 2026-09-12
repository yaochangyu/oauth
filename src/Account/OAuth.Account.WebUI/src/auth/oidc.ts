import { UserManager, WebStorageStateStore, type User } from 'oidc-client-ts'

const AUTHORITY = (import.meta.env.VITE_AUTHORITY as string) || 'https://localhost:7001'
const CLIENT_ID = (import.meta.env.VITE_CLIENT_ID as string) || 'account-portal'
const REDIRECT_URI = (import.meta.env.VITE_REDIRECT_URI as string) || `${window.location.origin}/callback`
const POST_LOGOUT_URI = (import.meta.env.VITE_POST_LOGOUT_URI as string) || `${window.location.origin}/`

export const userManager = new UserManager({
  authority: AUTHORITY,
  client_id: CLIENT_ID,
  redirect_uri: REDIRECT_URI,
  post_logout_redirect_uri: POST_LOGOUT_URI,
  response_type: 'code',
  scope: 'openid profile email offline_access api',
  userStore: new WebStorageStateStore({ store: window.localStorage }),
  automaticSilentRenew: true,
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

export async function getAccessToken(): Promise<string | null> {
  const user = await userManager.getUser()
  return user && !user.expired ? user.access_token : null
}
