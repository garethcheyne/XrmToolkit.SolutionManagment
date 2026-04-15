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

  // Target data from C# (targets have separate auth, React can't query directly)
  targetSolutions: (connectionName: string, json: string) => void;
  targetFlows: (connectionName: string, json: string) => void;
  targetEnvVars: (connectionName: string, json: string) => void;
  targetOrgSettings: (connectionName: string, json: string) => void;

  // Plugin settings persistence
  loadPluginSettings: (json: string) => void;

  // Results from C# services
  transferResult: (json: string) => void;
  flowResults: (json: string) => void;
  missingDeps: (json: string) => void;

  // Alerts (replaces WinForms MessageBox)
  showAlert: (title: string, message: string, severity: string) => void;

  // Tab navigation
  setActiveTab: (tab: string) => void;

  // Progress (write operations still managed by C#)
  setProgressItems: (json: string) => void;
  updateProgressItem: (json: string) => void;
  showProgress: (visible: boolean) => void;
  showRetryButton: (show: boolean) => void;

  // Active imports pre-flight
  activeImportsDetected: (json: string) => void;
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

  // Target data from C#
  targetSolutions: (cn: string, json: string) => callOrBuffer('targetSolutions', [cn, json]),
  targetFlows: (cn: string, json: string) => callOrBuffer('targetFlows', [cn, json]),
  targetEnvVars: (cn: string, json: string) => callOrBuffer('targetEnvVars', [cn, json]),
  targetOrgSettings: (cn: string, json: string) => callOrBuffer('targetOrgSettings', [cn, json]),

  // Plugin settings
  loadPluginSettings: (json: string) => callOrBuffer('loadPluginSettings', [json]),

  // Results from C# services
  transferResult: (json: string) => callOrBuffer('transferResult', [json]),
  flowResults: (json: string) => callOrBuffer('flowResults', [json]),
  missingDeps: (json: string) => callOrBuffer('missingDeps', [json]),

  // Alerts (replaces WinForms MessageBox)
  showAlert: (title: string, message: string, severity: string) =>
    callOrBuffer('showAlert', [title, message, severity]),

  // Progress (C# manages these for write operations)
  setProgressItems: (json: string) => callOrBuffer('setProgressItems', [json]),
  updateProgressItem: (json: string) => callOrBuffer('updateProgressItem', [json]),
  showProgress: (visible: boolean) => callOrBuffer('showProgress', [visible]),
  showRetryButton: (show: boolean) => callOrBuffer('showRetryButton', [show]),

  // Active imports pre-flight
  activeImportsDetected: (json: string) => callOrBuffer('activeImportsDetected', [json]),

  // Tab
  setActiveTab: (tab: string) => callOrBuffer('setActiveTab', [tab]),
};

window.bridge = bridgeApi;
