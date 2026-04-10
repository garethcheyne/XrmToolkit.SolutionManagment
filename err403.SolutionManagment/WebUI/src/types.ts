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

// ── Bridge message types ──
// Only write operations go through C# (SDK-only operations).
// All reads are done directly by React via Dataverse Web API.

export type BridgeMessage =
  // Solutions (SDK write operations)
  | { action: 'transferSolutions'; solutions: SelectedSolution[] }
  | { action: 'transferWithSettings'; solutions: SelectedSolution[] }
  | { action: 'importFromFile' }
  | { action: 'removeFromTargets'; solutions: SelectedSolution[] }
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
  // Progress
  | { action: 'downloadLog'; id: string }
  | { action: 'viewMessage'; id: string }
  | { action: 'downloadSolutionFile'; id: string }
  | { action: 'retryTransfer' };

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
