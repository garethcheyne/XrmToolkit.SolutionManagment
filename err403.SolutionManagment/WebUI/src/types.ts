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
}

export interface TransferResult {
  solution: string;
  target: string;
  success: boolean;
  error: string;
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
  | { action: 'findMissingDeps' }
  // Cloud Flows (SDK write: SetState)
  | { action: 'activateFlows'; flows: SelectedFlow[] }
  | { action: 'deactivateFlows'; flows: SelectedFlow[] }
  // Connection management
  | { action: 'tabChanged'; tab: string }
  | { action: 'addTarget' }
  | { action: 'removeTarget'; connectionName: string }
  | { action: 'refreshToken' }
  | { action: 'openUrl'; url: string }
  // Settings persistence
  | { action: 'savePluginSettings'; settings: string };

export interface SelectedSolution {
  solutionId: string;
  uniqueName: string;
  friendlyName: string;
  version: string;
}

export interface SelectedFlow {
  name: string;
  workflowId: string;
  stateCode: number;
}
