import { getAuth } from './auth';

// ── Dataverse Web API client ──
// All tabs call these functions directly. No C# bridge needed for reads.

const API_VERSION = 'v9.2';

async function apiFetch<T>(path: string, options?: RequestInit): Promise<T> {
  const auth = getAuth();
  if (!auth) throw new Error('Not connected');

  const url = `${auth.orgUrl}/api/data/${API_VERSION}/${path}`;

  const res = await fetch(url, {
    ...options,
    headers: {
      Authorization: `Bearer ${auth.token}`,
      'OData-MaxVersion': '4.0',
      'OData-Version': '4.0',
      Accept: 'application/json',
      'Content-Type': 'application/json',
      Prefer: 'odata.include-annotations="*"',
      ...options?.headers,
    },
  });

  if (res.status === 401) {
    // Token expired — request refresh from C#
    if (window.chrome?.webview) {
      window.chrome.webview.postMessage(JSON.stringify({ action: 'refreshToken' }));
    }
    throw new Error('Authentication expired. Refreshing...');
  }

  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Dataverse API error ${res.status}: ${body}`);
  }

  if (res.status === 204) return {} as T;
  return res.json();
}

interface ODataResponse<T> {
  value: T[];
}

// ── Solutions ──

export interface SolutionRecord {
  solutionid: string;
  uniquename: string;
  friendlyname: string;
  version: string;
  installedon: string;
  description: string;
  ismanaged: boolean;
  _publisherid_value: string;
  publisherid?: { friendlyname?: string };
}

export async function getSolutions() {
  const data = await apiFetch<ODataResponse<SolutionRecord>>(
    `solutions?$select=solutionid,uniquename,friendlyname,version,installedon,description,ismanaged,_publisherid_value` +
      `&$filter=ismanaged eq false and isvisible eq true and uniquename ne 'Default' and uniquename ne 'Active' and uniquename ne 'Basic'` +
      `&$orderby=friendlyname asc` +
      `&$expand=publisherid($select=friendlyname)`
  );
  return data.value;
}

export async function getTargetSolutions(orgUrl: string, token: string, uniqueNames: string[]) {
  if (uniqueNames.length === 0) return [];

  // Use fetchXml for large lists to avoid URL length limits
  const values = uniqueNames.map((n) => `<value>${n}</value>`).join('');
  const fetchXml = `<fetch><entity name="solution"><attribute name="uniquename"/><attribute name="version"/><attribute name="ismanaged"/><filter><condition attribute="uniquename" operator="in">${values}</condition></filter></entity></fetch>`;

  const url = `${orgUrl}/api/data/${API_VERSION}/solutions?fetchXml=${encodeURIComponent(fetchXml)}`;

  const res = await fetch(url, {
    headers: {
      Authorization: `Bearer ${token}`,
      'OData-MaxVersion': '4.0',
      'OData-Version': '4.0',
      Accept: 'application/json',
    },
  });
  if (!res.ok) throw new Error(`Target API error ${res.status}`);
  const data: ODataResponse<{ uniquename: string; version: string; ismanaged: boolean }> = await res.json();
  return data.value;
}

// ── Environment Variables ──

export interface EnvVarDefinition {
  environmentvariabledefinitionid: string;
  displayname: string;
  schemaname: string;
  description: string;
  type: number;
  defaultvalue: string;
}

export interface EnvVarValue {
  environmentvariablevalueid: string;
  value: string;
  _environmentvariabledefinitionid_value: string;
}

export async function getEnvVarDefinitions() {
  const data = await apiFetch<ODataResponse<EnvVarDefinition>>(
    `environmentvariabledefinitions?$select=displayname,schemaname,description,type,defaultvalue` +
      `&$filter=statecode eq 0&$orderby=displayname asc`
  );
  return data.value;
}

export async function getEnvVarValues() {
  const data = await apiFetch<ODataResponse<EnvVarValue>>(
    `environmentvariablevalues?$select=value,_environmentvariabledefinitionid_value` +
      `&$filter=statecode eq 0`
  );
  return data.value;
}

// ── Cloud Flows (Workflows) ──

export interface WorkflowRecord {
  workflowid: string;
  name: string;
  category: number;
  statecode: number;
  statuscode: number;
  _ownerid_value: string;
  'ownerid@OData.Community.Display.V1.FormattedValue'?: string;
  modifiedon: string;
  'solution.friendlyname'?: string;
}

export async function getCloudFlows() {
  const data = await apiFetch<ODataResponse<WorkflowRecord>>(
    `workflows?$select=workflowid,name,category,statecode,statuscode,_ownerid_value,modifiedon` +
      `&$filter=category eq 5 and type eq 1` +
      `&$orderby=name asc`
  );
  return data.value;
}

// ── Platform / Organization Settings ──

export interface OrgSettingRecord {
  [key: string]: unknown;
  organizationid?: string;
}

export interface SettingDefinition {
  uniquename: string;
  displayname: string;
  description: string;
  settingtype: string; // Boolean, Number, String, Enum
  defaultvalue: string;
  groupname: string;
  isoverridable: boolean;
}

export async function getOrgSettings() {
  const data = await apiFetch<OrgSettingRecord>(
    `organizations?$select=organizationid`
  );
  const values = (data as unknown as ODataResponse<OrgSettingRecord>).value;
  if (!values || values.length === 0) return {};
  const orgId = values[0]?.organizationid;
  if (!orgId) return {};

  const org = await apiFetch<OrgSettingRecord>(`organizations(${orgId})`);
  return org;
}

export async function getSettingDefinitions(): Promise<SettingDefinition[]> {
  try {
    // Try the RetrieveSettingList unbound function
    const data = await apiFetch<{ Settings: SettingDefinition[] }>('RetrieveSettingList()');
    return data.Settings ?? [];
  } catch {
    // Fallback: return empty if not available
    return [];
  }
}

// ── Write operations (still go through C# for SDK operations) ──
// Solution transfer, import/export, flow activation use SDK messages
// that aren't available via Web API. These stay as bridge calls.
