import { useState, useEffect, useCallback } from 'react';
import {
  FluentProvider,
  webLightTheme,
  TabList,
  Tab,
  makeStyles,
  tokens,
  type SelectTabData,
} from '@fluentui/react-components';
import {
  CloudFlowRegular,
  GridRegular,
  SettingsRegular,
  DatabaseRegular,
  BookRegular,
} from '@fluentui/react-icons';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { SolutionsTab } from './tabs/SolutionsTab';
import { EnvironmentVarsTab } from './tabs/EnvironmentVarsTab';
import { CloudFlowsTab } from './tabs/CloudFlowsTab';
import { PlatformSettingsTab } from './tabs/PlatformSettingsTab';
import { DocsViewer } from './tabs/DocsViewer';
import { ConnectionBar } from './components/ConnectionBar';
import { AboutDialog } from './dialogs/AboutDialog';
import { AlertDialog, emptyAlert, type AlertDialogState } from './dialogs/AlertDialog';
import { ActiveImportsDialog, type ActiveImportInfo } from './dialogs/ActiveImportsDialog';
import type { ProgressItemData } from './panels/ProgressPanel';
import { defaultSettings, type PluginSettings } from './panels/SettingsPanel';
import { setBridgeHandler, postMessage } from './bridge';
import { onAuthChange, type AuthContext } from './auth';
import type { SourceConnection, TargetConnection } from './types';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000, // Data is fresh for 30 seconds
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

const useStyles = makeStyles({
  root: {
    display: 'flex',
    flexDirection: 'column',
    height: '100vh',
    overflow: 'hidden',
    backgroundColor: tokens.colorNeutralBackground1,
  },
  tabBar: {
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground2,
    paddingLeft: '8px',
    flexShrink: 0,
  },
  body: {
    display: 'flex',
    flex: 1,
    overflow: 'hidden',
  },
  content: {
    flex: 1,
    overflow: 'hidden',
  },
});

type TabId = 'solutions' | 'envvars' | 'flows' | 'settings' | 'help';

export function App() {
  const styles = useStyles();
  const [activeTab, setActiveTab] = useState<TabId>('solutions');
  const [source, setSource] = useState<SourceConnection>({
    name: '',
    isConnected: false,
  });
  const [targets, setTargets] = useState<TargetConnection[]>([]);
  const [progressItems, setProgressItems] = useState<ProgressItemData[]>([]);
  const [progressVisible, setProgressVisible] = useState(false);
  const [showRetry, setShowRetry] = useState(false);
  const [aboutOpen, setAboutOpen] = useState(false);
  const [alertState, setAlertState] = useState<AlertDialogState>(emptyAlert);
  const [targetSolutionData, setTargetSolutionData] = useState<Record<string, Array<{ uniquename: string; version: string; ismanaged: boolean }>>>({});
  const [targetFlowData, setTargetFlowData] = useState<Record<string, Array<{ name: string; statecode: number; statuscode: number }>>>({});
  const [targetEnvVarData, setTargetEnvVarData] = useState<Record<string, Array<{ schemaname: string; value: string; exists: boolean }>>>({});
  const [targetOrgSettingsData, setTargetOrgSettingsData] = useState<Record<string, Record<string, unknown>>>({});
  const [pluginSettings, setPluginSettings] = useState<PluginSettings>(defaultSettings);
  const [authContext, setAuthContext] = useState<AuthContext | null>(null);
  const [activeImportsData, setActiveImportsData] = useState<Record<string, ActiveImportInfo[]>>({});
  const [activeImportsOpen, setActiveImportsOpen] = useState(false);

  useEffect(() => {
    return onAuthChange((auth) => setAuthContext(auth));
  }, []);

  const updateItem = useCallback((updated: ProgressItemData) => {
    setProgressItems((prev) => {
      const exists = prev.some((item) => item.id === updated.id);
      if (exists) {
        return prev.map((item) => (item.id === updated.id ? updated : item));
      }
      return [...prev, updated];
    });
    setProgressVisible(true);
  }, []);

  useEffect(() => {
    setBridgeHandler('setActiveTab', (tab: string) => {
      setActiveTab(tab as TabId);
    });

    setBridgeHandler('setSource', (name: string, isConnected: boolean) => {
      setSource({ name, isConnected });
      if (isConnected) {
        // Invalidate queries when source changes — fresh data needed
        queryClient.invalidateQueries();
      }
    });

    setBridgeHandler('setAuthContext', () => {
      // Auth context is handled by auth.ts (setAuth called from bridge.ts)
      // Invalidate all queries to refetch with new auth
      queryClient.invalidateQueries();
    });

    setBridgeHandler('addTargetContext', (name: string, orgUrl: string, token: string, envId: string) => {
      setTargets((prev) => {
        if (prev.some((t) => t.name === name)) return prev;
        return [...prev, { name, orgUrl, token, environmentId: envId || null }];
      });
    });

    setBridgeHandler('removeTarget', (connectionName: string) => {
      setTargets((prev) => prev.filter((t) => t.name !== connectionName));
    });

    setBridgeHandler('setTargets', (json: string) => {
      const parsed: TargetConnection[] = JSON.parse(json);
      setTargets(parsed);
    });

    // Load saved plugin settings from C#
    setBridgeHandler('loadPluginSettings', (json: string) => {
      try {
        const saved: Partial<PluginSettings> = JSON.parse(json);
        // Ensure solutionProfiles is always an object, never null
        if (!saved.solutionProfiles || typeof saved.solutionProfiles !== 'object') {
          saved.solutionProfiles = {};
        }
        setPluginSettings((prev) => ({ ...prev, ...saved }));
      } catch { /* ignore parse errors */ }
    });

    // Target data from C# (persists across tab switches)
    setBridgeHandler('targetSolutions', (connectionName: string, json: string) => {
      const solutions: Array<{ uniquename: string; version: string; ismanaged: boolean }> = JSON.parse(json);
      setTargetSolutionData((prev) => ({ ...prev, [connectionName]: solutions }));
    });

    setBridgeHandler('targetFlows', (connectionName: string, json: string) => {
      const flows: Array<{ name: string; statecode: number; statuscode: number }> = JSON.parse(json);
      setTargetFlowData((prev) => ({ ...prev, [connectionName]: flows }));
    });

    setBridgeHandler('targetEnvVars', (connectionName: string, json: string) => {
      const vars: Array<{ schemaname: string; value: string; exists: boolean }> = JSON.parse(json);
      setTargetEnvVarData((prev) => ({ ...prev, [connectionName]: vars }));
    });

    setBridgeHandler('targetOrgSettings', (connectionName: string, json: string) => {
      const settings: Record<string, unknown> = JSON.parse(json);
      setTargetOrgSettingsData((prev) => ({ ...prev, [connectionName]: settings }));
    });

    // Alerts from C# (replaces WinForms MessageBox)
    setBridgeHandler('showAlert', (title: string, message: string, severity: string) => {
      setAlertState({
        open: true,
        title,
        message,
        severity: (severity as AlertDialogState['severity']) || 'info',
      });
    });

    setBridgeHandler('setProgressItems', (json: string) => {
      const items: ProgressItemData[] = JSON.parse(json);
      setProgressItems(items);
      setProgressVisible(true);
      setShowRetry(false);
    });

    setBridgeHandler('updateProgressItem', (json: string) => {
      const updated: ProgressItemData = JSON.parse(json);
      updateItem(updated);
    });

    setBridgeHandler('showProgress', (visible: boolean) => {
      setProgressVisible(visible);
    });

    setBridgeHandler('showRetryButton', (show: boolean) => {
      setShowRetry(show);
    });

    setBridgeHandler('activeImportsDetected', (json: string) => {
      const data: Record<string, ActiveImportInfo[]> = JSON.parse(json);
      setActiveImportsData(data);
      setActiveImportsOpen(true);
    });
  }, [updateItem]);

  const handleSettingsChange = useCallback((newSettings: PluginSettings) => {
    setPluginSettings(newSettings);
    // Persist to C#
    postMessage({ action: 'savePluginSettings', settings: JSON.stringify(newSettings) });
  }, []);

  const handleTabSelect = (_event: unknown, data: SelectTabData) => {
    const tab = data.value as TabId;
    setActiveTab(tab);
    postMessage({ action: 'tabChanged', tab });
  };

  return (
    <QueryClientProvider client={queryClient}>
      <FluentProvider theme={webLightTheme}>
        <div className={styles.root}>
          <ConnectionBar source={source} targets={targets} hasEnvironmentId={!!authContext?.environmentId} onAbout={() => setAboutOpen(true)} />
          <div className={styles.tabBar}>
            <TabList
              selectedValue={activeTab}
              onTabSelect={handleTabSelect}
              size="small"
            >
              <Tab value="solutions" icon={<GridRegular />}>
                Solutions
              </Tab>
              <Tab value="envvars" icon={<DatabaseRegular />}>
                Environment Variables
              </Tab>
              <Tab value="flows" icon={<CloudFlowRegular />}>
                Cloud Flows
              </Tab>
              <Tab value="settings" icon={<SettingsRegular />}>
                Platform Settings
              </Tab>
              <Tab value="help" icon={<BookRegular />}>
                Help
              </Tab>
            </TabList>
          </div>
          <div className={styles.body}>
            <div className={styles.content}>
              {activeTab === 'solutions' && <SolutionsTab targets={targets} targetSolutionData={targetSolutionData}
                pluginSettings={pluginSettings} onSettingsChange={handleSettingsChange}
                progressItems={progressItems} progressVisible={progressVisible}
                showRetry={showRetry} onProgressClose={() => setProgressVisible(false)} />}
              {activeTab === 'envvars' && <EnvironmentVarsTab targets={targets} targetEnvVarData={targetEnvVarData} />}
              {activeTab === 'flows' && <CloudFlowsTab targets={targets} targetFlowData={targetFlowData} />}
              {activeTab === 'settings' && <PlatformSettingsTab targets={targets} targetOrgSettingsData={targetOrgSettingsData} />}
              {activeTab === 'help' && <DocsViewer />}
            </div>
          </div>
        </div>
        <AboutDialog open={aboutOpen} onClose={() => setAboutOpen(false)} />
        <AlertDialog state={alertState} onClose={() => setAlertState(emptyAlert)} />
        <ActiveImportsDialog
          open={activeImportsOpen}
          activeImports={activeImportsData}
          onClose={() => setActiveImportsOpen(false)}
        />
      </FluentProvider>
    </QueryClientProvider>
  );
}
