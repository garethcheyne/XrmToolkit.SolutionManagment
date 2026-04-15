// ── Connection types ──

export interface SourceConnection {
  name: string;
  isConnected: boolean;
}

export interface TargetConnection {
  name: string;
  orgUrl: string;
  token: string;
  environmentId: string | null;
}

// ── Transfer settings ──

export interface TransferSettings {
  managed: boolean;
  importMode: 'Update' | 'StageForUpgrade' | 'Upgrade';
  overwriteUnmanaged: boolean;
  publishWorkflows: boolean;
  checkDependencies: boolean;
  convertToManaged: boolean;
  skipProductUpdateDeps: boolean;
  autoNumbering: boolean;
  calendarSettings: boolean;
  customizationSettings: boolean;
  emailTracking: boolean;
  externalApps: boolean;
  generalSettings: boolean;
  isvConfig: boolean;
  marketingSettings: boolean;
  outlookSync: boolean;
  relationshipRoles: boolean;
  sales: boolean;
  // Per-solution overrides (key = uniqueName)
  perSolution?: Record<string, Omit<TransferSettings, 'perSolution'>>;
}

export interface TransferResult {
  solution: string;
  target: string;
  success: boolean;
  error: string;
  elapsedMs?: number;
}

export interface FlowResult {
  FlowName: string;
  TargetName: string;
  Success: boolean;
  ErrorMessage: string;
  IsConnectionRefError: boolean;
  // Aliases for React display
  flowName?: string;
  targetName?: string;
  success?: boolean;
  errorMessage?: string;
}

// ── Bridge message types ──

export type BridgeMessage =
  // Solutions (SDK write operations)
  | { action: 'startTransfer'; solutions: SelectedSolution[]; settings: TransferSettings }
  | { action: 'importFromFile' }
  | { action: 'exportToFile'; solutions: SelectedSolution[] }
  | { action: 'removeFromTargets'; solutions: SelectedSolution[] }
  | { action: 'removeFromSource'; solutions: SelectedSolution[] }
  | { action: 'switchOrgs' }
  | { action: 'findMissingDeps'; solutions: SelectedSolution[] }
  | { action: 'openSolutionInMaker'; solutionId: string }
  // Cloud Flows (SDK write: SetState)
  | { action: 'activateFlows'; flows: SelectedFlow[]; targetName?: string }
  | { action: 'deactivateFlows'; flows: SelectedFlow[]; targetName?: string }
  | { action: 'openFlowInMaker'; flowIds: string[]; connectionName?: string }
  // Environment Variables (SDK write)
  | { action: 'transferEnvVars'; items: EnvVarTransferItem[] }
  | { action: 'saveEnvVar'; schemaName: string; displayName: string; changedValues: Record<string, string> }
  | { action: 'refreshEnvVars' }
  // Platform Settings (SDK write: organizationsetting create/update)
  | { action: 'syncSettings'; items: SettingSyncItem[]; all: boolean }
  | { action: 'refreshSettings' }
  // Connection management
  | { action: 'tabChanged'; tab: string }
  | { action: 'addTarget' }
  | { action: 'removeTarget'; connectionName: string }
  | { action: 'refreshToken' }
  | { action: 'openUrl'; url: string }
  | { action: 'authenticateGds' }
  // Progress actions
  | { action: 'downloadLog'; id: string }
  | { action: 'viewMessage'; id: string }
  | { action: 'downloadSolution'; id: string }
  | { action: 'retryTransfer' }
  // Settings persistence
  | { action: 'savePluginSettings'; settings: string }
  // Help
  | { action: 'popOutHelp' }
  // Active imports pre-flight
  | { action: 'activeImportsResponse'; skipTargets: string[]; waitTargets: string[] };

export interface EnvVarTransferItem {
  schemaName: string;
  displayName: string;
  sourceValue: string;
  definitionId: string;
}

export interface SettingSyncItem {
  uniqueName: string;
  displayName: string;
  sourceValue: string;
}

export interface SelectedSolution {
  solutionId: string;
  uniqueName: string;
  friendlyName: string;
  version: string;
  newVersion?: string;
}

export interface SelectedFlow {
  name: string;
  workflowId: string;
  stateCode: number;
}
