import type { BridgeMessage } from './types';
import { setAuth } from './auth';

// ── JS → C# (write operations + connection management) ──
export function postMessage(message: BridgeMessage): void {
  if (window.chrome?.webview) {
    window.chrome.webview.postMessage(JSON.stringify(message));
  } else {
    console.log('[bridge] postMessage:', message);
  }
}

// ── Extend Window for C# → JS calls ──
declare global {
  interface Window {
    chrome?: {
      webview?: {
        postMessage: (message: string) => void;
      };
    };
    bridge: typeof bridgeApi;
  }
}

// ── C# → JS handlers ──
// Massively slimmed down: C# only sends auth context and target connections.
// All data fetching is done by React directly via Dataverse Web API.

type BridgeHandlers = {
  // Auth context (orgUrl + token)
  setAuthContext: (orgUrl: string, token: string, environmentId: string) => void;

  // Target connections (each with their own orgUrl + token)
  addTargetContext: (connectionName: string, orgUrl: string, token: string, environmentId: string) => void;
  removeTarget: (connectionName: string) => void;
  setTargets: (json: string) => void;

  // Source display name (for ConnectionBar)
  setSource: (connectionName: string, isConnected: boolean) => void;

  // Tab navigation
  setActiveTab: (tab: string) => void;

  // Progress (write operations still managed by C#)
  setProgressItems: (json: string) => void;
  updateProgressItem: (json: string) => void;
  showProgress: (visible: boolean) => void;
  showRetryButton: (show: boolean) => void;
};

const handlers: Partial<BridgeHandlers> = {};

// Buffer calls that arrive before React registers handlers
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const pendingCalls: Array<{ key: string; args: any[] }> = [];

export function setBridgeHandler<K extends keyof BridgeHandlers>(
  key: K,
  handler: BridgeHandlers[K]
): void {
  handlers[key] = handler;

  // Replay any buffered calls for this handler
  const toReplay = pendingCalls.filter((c) => c.key === key);
  for (const call of toReplay) {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (handler as (...args: any[]) => void)(...call.args);
  }
  for (let i = pendingCalls.length - 1; i >= 0; i--) {
    if (pendingCalls[i]?.key === key) {
      pendingCalls.splice(i, 1);
    }
  }
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function callOrBuffer(key: string, args: any[]): void {
  const handler = handlers[key as keyof BridgeHandlers];
  if (handler) {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (handler as (...a: any[]) => void)(...args);
  } else {
    pendingCalls.push({ key, args });
  }
}

// ── Bridge API exposed on window ──
const bridgeApi = {
  // Auth — sets the Dataverse connection context for direct API calls
  setAuthContext: (orgUrl: string, token: string, environmentId: string) => {
    setAuth({ orgUrl, token, environmentId: environmentId || null });
    callOrBuffer('setAuthContext', [orgUrl, token, environmentId]);
  },

  // Source display
  setSource: (name: string, connected: boolean) =>
    callOrBuffer('setSource', [name, connected]),

  // Targets
  addTargetContext: (name: string, orgUrl: string, token: string, envId: string) =>
    callOrBuffer('addTargetContext', [name, orgUrl, token, envId]),
  removeTarget: (name: string) => callOrBuffer('removeTarget', [name]),
  setTargets: (json: string) => callOrBuffer('setTargets', [json]),

  // Progress (C# manages these for write operations)
  setProgressItems: (json: string) => callOrBuffer('setProgressItems', [json]),
  updateProgressItem: (json: string) => callOrBuffer('updateProgressItem', [json]),
  showProgress: (visible: boolean) => callOrBuffer('showProgress', [visible]),
  showRetryButton: (show: boolean) => callOrBuffer('showRetryButton', [show]),

  // Tab
  setActiveTab: (tab: string) => callOrBuffer('setActiveTab', [tab]),
};

window.bridge = bridgeApi;
