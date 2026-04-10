// Auth context received from C# via bridge
// C# calls window.bridge.setAuthContext() when connection is established

export interface AuthContext {
  orgUrl: string; // e.g. "https://contoso.crm6.dynamics.com"
  token: string; // Bearer token
  environmentId: string | null;
}

let currentAuth: AuthContext | null = null;
const listeners: Array<(auth: AuthContext) => void> = [];

export function setAuth(auth: AuthContext): void {
  currentAuth = auth;
  listeners.forEach((fn) => fn(auth));
}

export function getAuth(): AuthContext | null {
  return currentAuth;
}

export function onAuthChange(fn: (auth: AuthContext) => void): () => void {
  listeners.push(fn);
  return () => {
    const idx = listeners.indexOf(fn);
    if (idx >= 0) listeners.splice(idx, 1);
  };
}

// Request a fresh token from C# (token may have expired)
export async function refreshToken(): Promise<string> {
  if (window.chrome?.webview) {
    window.chrome.webview.postMessage(
      JSON.stringify({ action: 'refreshToken' })
    );
  }
  // C# will call setAuthContext with a new token
  // For now return the current token; real refresh happens async
  return currentAuth?.token ?? '';
}
