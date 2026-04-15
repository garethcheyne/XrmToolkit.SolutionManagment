import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  DataGrid, DataGridHeader, DataGridRow, DataGridHeaderCell,
  DataGridBody, DataGridCell, createTableColumn,
  type TableColumnDefinition, type DataGridProps, type TableColumnSizingOptions,
  SearchBox, Text, Toolbar, ToolbarButton, ToolbarDivider,
  Badge, Menu, MenuTrigger, MenuPopover, MenuList, MenuItem,
  Tooltip, tokens, makeStyles, type SelectionItemId,
  Popover, PopoverTrigger, PopoverSurface, Link, Button,
} from '@fluentui/react-components';
import {
  ArrowSyncRegular, ArrowUploadRegular, ArrowDownloadRegular,
  DeleteRegular, ArrowSwapRegular,
  SearchRegular, OpenRegular, FolderOpenRegular, SettingsRegular,
  LockClosedFilled, LockOpenFilled, InfoRegular,
} from '@fluentui/react-icons';
import { TableSkeleton } from '../components/TableSkeleton';
import { useQuery } from '@tanstack/react-query';
import { getSolutions } from '../dataverse';
import { getAuth } from '../auth';
import { postMessage, setBridgeHandler } from '../bridge';
import { SettingsPanel, type PluginSettings } from '../panels/SettingsPanel';
import { ProgressPanel, type ProgressItemData } from '../panels/ProgressPanel';
import { Allotment } from 'allotment';
import 'allotment/dist/style.css';
import { TransferConfirmDialog } from '../dialogs/TransferConfirmDialog';
import { TransferResultsDialog } from '../dialogs/TransferResultsDialog';
import { ConfirmDialog, emptyConfirm, type ConfirmDialogState } from '../dialogs/ConfirmDialog';
import { MissingDepsDialog, type MissingDepResult } from '../dialogs/MissingDepsDialog';
import type { TargetConnection, TransferResult } from '../types';

const useStyles = makeStyles({
  root: { display: 'flex', flexDirection: 'column', height: '100%', overflow: 'hidden' },
  toolbar: {
    padding: '4px 8px 4px 12px', borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground2, flexShrink: 0,
  },
  searchRow: {
    display: 'flex', alignItems: 'center', gap: '12px',
    padding: '4px 12px', borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground2, flexShrink: 0,
  },
  searchBox: { flex: '1 1 auto', maxWidth: '280px', minWidth: '150px' },
  gridContainer: { height: '100%', overflowX: 'auto', overflowY: 'auto' },
  headerCell: { fontWeight: 700, fontSize: '12px', backgroundColor: tokens.colorNeutralBackground3 },
  cell: { overflow: 'hidden', whiteSpace: 'nowrap', textOverflow: 'ellipsis', maxWidth: '100%' },
  versionCell: {
    display: 'inline-flex',
    alignItems: 'center',
    gap: '6px',
  },
  statusDot: {
    width: '18px',
    height: '18px',
    borderRadius: '50%',
    display: 'inline-flex',
    alignItems: 'center',
    justifyContent: 'center',
    flexShrink: 0,
  },
  statusMatch: {
    backgroundColor: tokens.colorPaletteGreenBackground1,
    border: `2px solid ${tokens.colorPaletteGreenBorder1}`,
  },
  statusMismatch: {
    backgroundColor: tokens.colorPaletteRedBackground1,
    border: `2px solid ${tokens.colorPaletteRedBorder1}`,
  },
  dotInner: {
    width: '8px',
    height: '8px',
    borderRadius: '50%',
  },
  dotGreen: {
    backgroundColor: tokens.colorPaletteGreenForeground1,
  },
  dotRed: {
    backgroundColor: tokens.colorPaletteRedForeground1,
  },
  notFoundCell: {
    color: tokens.colorNeutralForeground4,
    fontStyle: 'italic',
    fontSize: '12px',
  },
  legend: {
    display: 'flex',
    alignItems: 'center',
    gap: '12px',
    marginLeft: 'auto',
    fontSize: '11px',
    color: tokens.colorNeutralForeground3,
  },
  legendItem: {
    display: 'flex',
    alignItems: 'center',
    gap: '4px',
  },
  countBadge: { marginLeft: 'auto' },
  emptyState: {
    display: 'flex', flexDirection: 'column', alignItems: 'center',
    justifyContent: 'center', flex: 1, padding: '60px 20px', gap: '8px',
    color: tokens.colorNeutralForeground3,
  },
  loadingState: {
    display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center',
    height: '100%', gap: '12px',
  },
});

interface SolutionsTabProps {
  targets: TargetConnection[];
  targetSolutionData: Record<string, Array<{ uniquename: string; version: string; ismanaged: boolean }>>;
  pluginSettings: PluginSettings;
  onSettingsChange: (settings: PluginSettings) => void;
  progressItems: ProgressItemData[];
  progressVisible: boolean;
  showRetry: boolean;
  onProgressClose: () => void;
}

interface SolutionRow {
  solutionId: string;
  uniqueName: string;
  friendlyName: string;
  version: string;
  installedOn: string;
  publisher: string;
  description: string;
  isManaged: boolean;
  targetVersions: Record<string, { version: string; isManaged: boolean }>;
}

export function SolutionsTab({ targets, targetSolutionData, pluginSettings, onSettingsChange, progressItems, progressVisible, showRetry, onProgressClose }: SolutionsTabProps) {
  const styles = useStyles();
  const auth = getAuth();
  const [search, setSearch] = useState('');
  const [selectedItems, setSelectedItems] = useState<Set<SelectionItemId>>(new Set());
  const [contextMenu, setContextMenu] = useState<{ solutionId: string; x: number; y: number } | null>(null);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [settingsOpen, setSettingsOpen] = useState(true);
  const [transferResults, setTransferResults] = useState<TransferResult[]>([]);
  const [resultsOpen, setResultsOpen] = useState(false);
  const [confirm, setConfirm] = useState<ConfirmDialogState>(emptyConfirm);
  const [missingDepsResults, setMissingDepsResults] = useState<MissingDepResult[]>([]);
  const [missingDepsOpen, setMissingDepsOpen] = useState(false);

  useEffect(() => {
    // Receive transfer results from C#
    setBridgeHandler('transferResult', (json: string) => {
      const result: TransferResult = JSON.parse(json);
      setTransferResults((prev) => {
        const updated = [...prev, result];
        setResultsOpen(true);
        return updated;
      });
    });

    // Receive missing deps check results from C#
    setBridgeHandler('missingDeps', (json: string) => {
      const results: MissingDepResult[] = JSON.parse(json);
      setMissingDepsResults(results);
      setMissingDepsOpen(true);
    });
  }, []);

  // Fetch solutions from source
  const { data: solutions = [], isLoading, refetch } = useQuery({
    queryKey: ['solutions', auth?.orgUrl],
    queryFn: getSolutions,
    enabled: !!auth,
  });

  // Build row data with target versions
  const rows: SolutionRow[] = useMemo(() =>
    solutions.map((s) => ({
      solutionId: s.solutionid,
      uniqueName: s.uniquename,
      friendlyName: s.friendlyname,
      version: s.version,
      installedOn: s.installedon ? new Date(s.installedon).toLocaleDateString('en-GB', { year: '2-digit', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' }) : '',
      publisher: s.publisherid?.friendlyname ?? '',
      description: s.description ?? '',
      isManaged: s.ismanaged,
      targetVersions: {},
    })),
    [solutions]
  );

  // Merge target data (received from C# via bridge) into rows
  const mergedRows = useMemo(() => {
    return rows.map((row) => {
      const tv: Record<string, { version: string; isManaged: boolean }> = {};
      targets.forEach((t) => {
        const tSols = targetSolutionData[t.name];
        if (tSols) {
          const match = tSols.find((ts) => ts.uniquename === row.uniqueName);
          if (match) {
            tv[t.name] = { version: match.version, isManaged: match.ismanaged };
          }
        }
      });
      return { ...row, targetVersions: tv };
    });
  }, [rows, targets, targetSolutionData]);

  const filteredRows = useMemo(() => {
    if (!search) return mergedRows;
    const lower = search.toLowerCase();
    return mergedRows.filter((s) =>
      s.uniqueName.toLowerCase().includes(lower) ||
      s.friendlyName.toLowerCase().includes(lower) ||
      s.publisher.toLowerCase().includes(lower)
    );
  }, [mergedRows, search]);

  const columnSizingOptions = useMemo<TableColumnSizingOptions>(() => {
    const opts: TableColumnSizingOptions = {
      uniqueName:    { minWidth: 160, defaultWidth: 220 },
      friendlyName:  { minWidth: 160, defaultWidth: 240 },
      version:       { minWidth: 80,  defaultWidth: 90  },
      installedOn:   { minWidth: 110, defaultWidth: 120 },
      publisher:     { minWidth: 120, defaultWidth: 160 },
    };
    for (const t of targets) {
      opts[`target_${t.name}`] = { minWidth: 120, defaultWidth: 150 };
    }
    return opts;
  }, [targets]);

  const columns = useMemo(() => {
    const cols: TableColumnDefinition<SolutionRow>[] = [
      createTableColumn({ columnId: 'uniqueName', compare: (a, b) => a.uniqueName.localeCompare(b.uniqueName),
        renderHeaderCell: () => 'Unique Name',
        renderCell: (item) => <Text truncate wrap={false} title={item.uniqueName} weight="semibold">{item.uniqueName}</Text>,
      }),
      createTableColumn({ columnId: 'friendlyName', compare: (a, b) => a.friendlyName.localeCompare(b.friendlyName),
        renderHeaderCell: () => 'Display Name',
        renderCell: (item) => <Text truncate wrap={false} title={item.friendlyName}>{item.friendlyName}</Text>,
      }),
      createTableColumn({ columnId: 'version', compare: (a, b) => a.version.localeCompare(b.version),
        renderHeaderCell: () => 'Version',
        renderCell: (item) => (
          <Badge size="small" appearance="tint" color="informative">
            {item.version}
          </Badge>
        ),
      }),
    ];

    for (const t of targets) {
      cols.push(createTableColumn({
        columnId: `target_${t.name}`,
        compare: (a, b) => (a.targetVersions[t.name]?.version ?? '').localeCompare(b.targetVersions[t.name]?.version ?? ''),
        renderHeaderCell: () => t.name,
        renderCell: (item) => {
          const tv = item.targetVersions[t.name];
          if (!tv) return <span className={styles.notFoundCell}>—</span>;
          const isMatch = tv.version === item.version;
          return (
            <span className={styles.versionCell}>
              <Tooltip content={tv.isManaged ? 'Managed' : 'Unmanaged'} relationship="label">
                <span>{tv.isManaged
                  ? <LockClosedFilled fontSize={16} color={tokens.colorBrandForeground1} />
                  : <LockOpenFilled fontSize={16} color={tokens.colorPaletteYellowForeground2} />
                }</span>
              </Tooltip>
              <Tooltip content={isMatch ? 'Version matches source' : 'Version differs from source'} relationship="label">
                <span className={`${styles.statusDot} ${isMatch ? styles.statusMatch : styles.statusMismatch}`}>
                  <span className={`${styles.dotInner} ${isMatch ? styles.dotGreen : styles.dotRed}`} />
                </span>
              </Tooltip>
              <Badge size="small" appearance="tint" color={isMatch ? 'success' : 'danger'}>
                {tv.version}
              </Badge>
            </span>
          );
        },
      }));
    }

    cols.push(
      createTableColumn({ columnId: 'installedOn', compare: (a, b) => a.installedOn.localeCompare(b.installedOn),
        renderHeaderCell: () => 'Installed', renderCell: (item) => item.installedOn,
      }),
      createTableColumn({ columnId: 'publisher', compare: (a, b) => a.publisher.localeCompare(b.publisher),
        renderHeaderCell: () => 'Publisher', renderCell: (item) => item.publisher,
      }),
    );
    return cols;
  }, [targets, styles]);

  const onSelectionChange: DataGridProps['onSelectionChange'] = useCallback(
    (e: unknown, data: { selectedItems: Set<SelectionItemId> }) => {
      const target = (e as React.SyntheticEvent)?.target as HTMLElement;
      if (target?.closest('[role="checkbox"]') || target?.closest('input[type="checkbox"]')) {
        setSelectedItems(data.selectedItems);
      }
    }, []);

  const getSelected = useCallback(() =>
    filteredRows.filter((s) => selectedItems.has(s.solutionId))
      .map((s) => ({ solutionId: s.solutionId, uniqueName: s.uniqueName, friendlyName: s.friendlyName, version: s.version })),
    [filteredRows, selectedItems]);

  if (isLoading) {
    return <TableSkeleton />;
  }

  return (
    <div className={styles.root}>
      <Toolbar className={styles.toolbar} size="small">
        <ToolbarButton icon={<ArrowSyncRegular />} onClick={() => refetch()}>Refresh</ToolbarButton>
        <ToolbarDivider />
        <ToolbarButton icon={<ArrowUploadRegular />} onClick={() => { if (getSelected().length > 0 && targets.length > 0) { setTransferResults([]); setConfirmOpen(true); } }}>Transfer</ToolbarButton>
        <ToolbarButton icon={<FolderOpenRegular />} onClick={() => postMessage({ action: 'importFromFile' })}>Import from File</ToolbarButton>
        <ToolbarDivider />
        <ToolbarButton icon={<ArrowDownloadRegular />} onClick={() => { const s = getSelected(); if (s.length) postMessage({ action: 'exportToFile', solutions: s }); }}>Export</ToolbarButton>
        <ToolbarButton icon={<DeleteRegular />} onClick={() => {
          const s = getSelected();
          if (s.length) setConfirm({
            open: true, title: 'Confirm Remove from Targets',
            message: `Remove ${s.length} solution(s) from ${targets.length} target(s)?`,
            severity: 'warning', confirmLabel: 'Remove',
            onConfirm: () => postMessage({ action: 'removeFromTargets', solutions: s }),
          });
        }}>Remove from Targets</ToolbarButton>
        <ToolbarButton icon={<DeleteRegular />} onClick={() => {
          const s = getSelected();
          if (s.length) setConfirm({
            open: true, title: 'Confirm Remove from Source',
            message: `Remove ${s.length} solution(s) from SOURCE environment? This cannot be undone.`,
            severity: 'danger', confirmLabel: 'Remove',
            onConfirm: () => postMessage({ action: 'removeFromSource', solutions: s }),
          });
        }}>Remove from Source</ToolbarButton>
        <ToolbarDivider />
        <ToolbarButton icon={<ArrowSwapRegular />} onClick={() => postMessage({ action: 'switchOrgs' })}>Switch</ToolbarButton>
        <ToolbarButton icon={<SearchRegular />} onClick={() => {
          const s = getSelected();
          if (s.length === 0) return;
          postMessage({ action: 'findMissingDeps', solutions: s });
        }}>Missing Deps</ToolbarButton>
        <Popover withArrow size="small">
          <PopoverTrigger>
            <Button appearance="subtle" size="small" icon={<InfoRegular fontSize={13} />}
              style={{ minWidth: 0, padding: '0 2px', alignSelf: 'center', color: tokens.colorNeutralForeground3 }}
              aria-label="About Missing Deps" />
          </PopoverTrigger>
          <PopoverSurface style={{ maxWidth: '280px' }}>
            <Text size={100} style={{ display: 'block', marginBottom: '6px' }}>
              Select one or more solutions, then click <strong>Missing Deps</strong> to check
              each target environment for required components that are not yet installed.
              The import will fail if missing components are not resolved first.
            </Text>
            <Link
              href="https://learn.microsoft.com/en-us/power-platform/alm/dependency-tracking-solution-components"
              target="_blank"
              style={{ fontSize: '11px' }}
            >
              Dependency tracking on MS Learn
            </Link>
          </PopoverSurface>
        </Popover>
        <ToolbarDivider />
        <ToolbarButton icon={<SettingsRegular />} onClick={() => setSettingsOpen(!settingsOpen)}
          appearance={settingsOpen ? 'primary' : 'subtle'}>Settings</ToolbarButton>
      </Toolbar>

      <div className={styles.searchRow}>
        <SearchBox className={styles.searchBox} placeholder="Search solutions..." value={search}
          onChange={(_e, data) => setSearch(data.value)} />
        <div className={styles.legend}>
          <span className={styles.legendItem}>
            <LockClosedFilled fontSize={14} color={tokens.colorBrandForeground1} />
            Managed
          </span>
          <span className={styles.legendItem}>
            <LockOpenFilled fontSize={14} color={tokens.colorPaletteYellowForeground2} />
            Unmanaged
          </span>
          <span className={styles.legendItem}>
            <Badge size="tiny" shape="circular" appearance="filled" color="success" />
            Match
          </span>
          <span className={styles.legendItem}>
            <Badge size="tiny" shape="circular" appearance="filled" color="danger" />
            Mismatch
          </span>
        </div>
        <Badge className={styles.countBadge} appearance="tint" color="informative" size="medium">
          {filteredRows.length} solution{filteredRows.length !== 1 ? 's' : ''}
          {selectedItems.size > 0 ? ` (${selectedItems.size} selected)` : ''}
        </Badge>
      </div>

      <Allotment proportionalLayout={false}>
          <Allotment.Pane>
            {filteredRows.length === 0 ? (
              <div className={styles.emptyState}>
                <Text size={400} weight="semibold">{!auth ? 'Not connected' : 'No solutions found'}</Text>
                <Text size={200}>{!auth ? 'Connect to a source environment first.' : 'Try adjusting your search.'}</Text>
              </div>
            ) : (
              <div className={styles.gridContainer}>
                <DataGrid items={filteredRows} columns={columns} sortable resizableColumns selectionMode="multiselect"
                  selectedItems={selectedItems} onSelectionChange={onSelectionChange}
                  getRowId={(item) => item.solutionId} focusMode="composite" size="small"
                  columnSizingOptions={columnSizingOptions}
                  style={{ minWidth: 'fit-content', width: '100%' }}>
                  <DataGridHeader style={{ position: 'sticky', top: 0, zIndex: 1, backgroundColor: tokens.colorNeutralBackground3, minWidth: 'fit-content' }}>
                    <DataGridRow>
                      {({ renderHeaderCell }) => <DataGridHeaderCell className={styles.headerCell}>{renderHeaderCell()}</DataGridHeaderCell>}
                    </DataGridRow>
                  </DataGridHeader>
                  <DataGridBody<SolutionRow>>
                    {({ item, rowId }) => (
                      <DataGridRow<SolutionRow> key={rowId}
                        onContextMenu={(e: React.MouseEvent) => { e.preventDefault(); setContextMenu({ solutionId: item.solutionId, x: e.clientX, y: e.clientY }); }}>
                        {({ renderCell }) => <DataGridCell className={styles.cell}>{renderCell(item)}</DataGridCell>}
                      </DataGridRow>
                    )}
                  </DataGridBody>
                </DataGrid>
              </div>
            )}
          </Allotment.Pane>
          {settingsOpen && (
            <Allotment.Pane preferredSize={320} minSize={250} maxSize={500}>
              <SettingsPanel
                open={settingsOpen}
                onClose={() => setSettingsOpen(false)}
                settings={pluginSettings}
                onSettingsChange={onSettingsChange}
                solutionNames={filteredRows.map((r) => r.uniqueName)}
                selectedSolution={getSelected()[0]?.uniqueName}
              />
            </Allotment.Pane>
          )}
          {progressVisible && (
            <Allotment.Pane preferredSize={340} minSize={280} maxSize={500}>
              <ProgressPanel
                items={progressItems}
                visible={progressVisible}
                showRetry={showRetry}
                onClose={onProgressClose}
              />
            </Allotment.Pane>
          )}
        </Allotment>

      {contextMenu && (
        <div style={{ position: 'fixed', left: contextMenu.x, top: contextMenu.y, zIndex: 1000 }}>
          <Menu open onOpenChange={() => setContextMenu(null)}>
            <MenuTrigger><span /></MenuTrigger>
            <MenuPopover><MenuList>
              <MenuItem icon={<OpenRegular />} disabled={!auth?.environmentId} onClick={() => {
                const envId = auth?.environmentId;
                if (envId) {
                  postMessage({ action: 'openUrl', url: `https://make.powerapps.com/environments/${envId}/solutions/${contextMenu.solutionId}` });
                } else {
                  postMessage({ action: 'authenticateGds' });
                }
                setContextMenu(null);
              }}>Open in Maker Portal{!auth?.environmentId ? ' (auth required)' : ''}</MenuItem>
            </MenuList></MenuPopover>
          </Menu>
        </div>
      )}

      <TransferConfirmDialog
        solutions={getSelected()}
        targets={targets}
        settings={pluginSettings}
        open={confirmOpen}
        onClose={() => setConfirmOpen(false)}
      />

      <TransferResultsDialog
        results={transferResults}
        open={resultsOpen}
        onClose={() => { setResultsOpen(false); refetch(); }}
      />

      <ConfirmDialog state={confirm} onClose={() => setConfirm(emptyConfirm)} />

      <MissingDepsDialog
        results={missingDepsResults}
        open={missingDepsOpen}
        onClose={() => setMissingDepsOpen(false)}
      />
    </div>
  );
}
