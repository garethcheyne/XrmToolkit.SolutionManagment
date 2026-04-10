import { useCallback, useMemo, useState } from 'react';
import {
  DataGrid, DataGridHeader, DataGridRow, DataGridHeaderCell,
  DataGridBody, DataGridCell, createTableColumn,
  type TableColumnDefinition, type DataGridProps,
  SearchBox, Text, Toolbar, ToolbarButton, ToolbarDivider,
  Badge, Spinner, Menu, MenuTrigger, MenuPopover, MenuList, MenuItem,
  tokens, makeStyles, type SelectionItemId,
} from '@fluentui/react-components';
import {
  ArrowSyncRegular, ArrowUploadRegular, ArrowDownloadRegular,
  DeleteRegular, ArrowSwapRegular,
  SearchRegular, OpenRegular, FolderOpenRegular,
} from '@fluentui/react-icons';
import { useQuery, useQueries } from '@tanstack/react-query';
import { getSolutions, getTargetSolutions } from '../dataverse';
import { getAuth } from '../auth';
import { postMessage } from '../bridge';
import type { TargetConnection } from '../types';

const useStyles = makeStyles({
  root: { display: 'flex', flexDirection: 'column', height: '100%', overflow: 'hidden' },
  toolbar: {
    padding: '4px 8px', borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground2, flexShrink: 0,
  },
  searchRow: {
    display: 'flex', alignItems: 'center', gap: '12px',
    padding: '6px 12px', borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground2, flexShrink: 0,
  },
  searchBox: { flexGrow: 1, maxWidth: '320px' },
  gridContainer: { flex: 1, overflow: 'auto' },
  matchCell: { color: tokens.colorPaletteGreenForeground1, fontWeight: 600 },
  mismatchCell: { color: tokens.colorPaletteRedForeground1, fontWeight: 600 },
  notFoundCell: { color: tokens.colorNeutralForeground4, fontStyle: 'italic' },
  countBadge: { marginLeft: 'auto' },
  emptyState: {
    display: 'flex', flexDirection: 'column', alignItems: 'center',
    justifyContent: 'center', flex: 1, padding: '60px 20px', gap: '8px',
    color: tokens.colorNeutralForeground3,
  },
  loadingState: {
    display: 'flex', alignItems: 'center', justifyContent: 'center',
    flex: 1, gap: '12px',
  },
});

interface SolutionsTabProps {
  targets: TargetConnection[];
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

export function SolutionsTab({ targets }: SolutionsTabProps) {
  const styles = useStyles();
  const auth = getAuth();
  const [search, setSearch] = useState('');
  const [selectedItems, setSelectedItems] = useState<Set<SelectionItemId>>(new Set());
  const [contextMenu, setContextMenu] = useState<{ solutionId: string; x: number; y: number } | null>(null);

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

  // Fetch target solutions for each target
  const uniqueNames = useMemo(() => rows.map((r) => r.uniqueName), [rows]);

  // Query target data for each connected target (useQueries handles dynamic count)
  const targetQueries = useQueries({
    queries: targets.map((t) => ({
      queryKey: ['targetSolutions', t.orgUrl, uniqueNames] as const,
      queryFn: () => getTargetSolutions(t.orgUrl, t.token, uniqueNames),
      enabled: !!t.token && uniqueNames.length > 0,
    })),
  });

  // Merge target data into rows
  const mergedRows = useMemo(() => {
    return rows.map((row) => {
      const tv: Record<string, { version: string; isManaged: boolean }> = {};
      targets.forEach((t, i) => {
        const tq = targetQueries[i];
        if (tq?.data) {
          const match = tq.data.find((ts) => ts.uniquename === row.uniqueName);
          if (match) {
            tv[t.name] = { version: match.version, isManaged: match.ismanaged };
          }
        }
      });
      return { ...row, targetVersions: tv };
    });
  }, [rows, targets, targetQueries]);

  const filteredRows = useMemo(() => {
    if (!search) return mergedRows;
    const lower = search.toLowerCase();
    return mergedRows.filter((s) =>
      s.uniqueName.toLowerCase().includes(lower) ||
      s.friendlyName.toLowerCase().includes(lower) ||
      s.publisher.toLowerCase().includes(lower)
    );
  }, [mergedRows, search]);

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
        renderHeaderCell: () => 'Version', renderCell: (item) => item.version,
      }),
    ];

    for (const t of targets) {
      cols.push(createTableColumn({
        columnId: `target_${t.name}`,
        compare: (a, b) => (a.targetVersions[t.name]?.version ?? '').localeCompare(b.targetVersions[t.name]?.version ?? ''),
        renderHeaderCell: () => t.name,
        renderCell: (item) => {
          const tv = item.targetVersions[t.name];
          if (!tv) return <Text className={styles.notFoundCell} size={200}>—</Text>;
          const isMatch = tv.version === item.version;
          return (
            <Text className={isMatch ? styles.matchCell : styles.mismatchCell} size={200}>
              <Badge size="tiny" appearance="filled" color={tv.isManaged ? 'brand' : 'informative'}
                style={{ marginRight: 4 }}>{tv.isManaged ? 'M' : 'U'}</Badge>
              {tv.version}
            </Text>
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
    (_e: unknown, data: { selectedItems: Set<SelectionItemId> }) => setSelectedItems(data.selectedItems), []);

  const getSelected = useCallback(() =>
    filteredRows.filter((s) => selectedItems.has(s.solutionId))
      .map((s) => ({ solutionId: s.solutionId, uniqueName: s.uniqueName, friendlyName: s.friendlyName, version: s.version })),
    [filteredRows, selectedItems]);

  if (isLoading) {
    return <div className={styles.loadingState}><Spinner size="small" /><Text>Loading solutions...</Text></div>;
  }

  return (
    <div className={styles.root}>
      <Toolbar className={styles.toolbar} size="small">
        <ToolbarButton icon={<ArrowSyncRegular />} onClick={() => refetch()}>Refresh</ToolbarButton>
        <ToolbarDivider />
        <ToolbarButton icon={<ArrowUploadRegular />} onClick={() => { const s = getSelected(); if (s.length) postMessage({ action: 'transferSolutions', solutions: s }); }}>Transfer</ToolbarButton>
        <ToolbarButton icon={<FolderOpenRegular />} onClick={() => postMessage({ action: 'importFromFile' })}>Import from File</ToolbarButton>
        <ToolbarDivider />
        <ToolbarButton icon={<ArrowDownloadRegular />} onClick={() => { /* export handled by C# */ }}>Export</ToolbarButton>
        <ToolbarButton icon={<DeleteRegular />} onClick={() => { const s = getSelected(); if (s.length) postMessage({ action: 'removeFromTargets', solutions: s }); }}>Remove</ToolbarButton>
        <ToolbarDivider />
        <ToolbarButton icon={<ArrowSwapRegular />} onClick={() => postMessage({ action: 'switchOrgs' })}>Switch</ToolbarButton>
        <ToolbarButton icon={<SearchRegular />} onClick={() => postMessage({ action: 'findMissingDeps' })}>Missing Deps</ToolbarButton>
      </Toolbar>

      <div className={styles.searchRow}>
        <SearchBox className={styles.searchBox} placeholder="Search solutions..." value={search}
          onChange={(_e, data) => setSearch(data.value)} />
        <Badge className={styles.countBadge} appearance="tint" color="informative" size="medium">
          {filteredRows.length} solution{filteredRows.length !== 1 ? 's' : ''}
          {selectedItems.size > 0 ? ` (${selectedItems.size} selected)` : ''}
        </Badge>
      </div>

      {filteredRows.length === 0 ? (
        <div className={styles.emptyState}>
          <Text size={400} weight="semibold">{!auth ? 'Not connected' : 'No solutions found'}</Text>
          <Text size={200}>{!auth ? 'Connect to a source environment first.' : 'Try adjusting your search.'}</Text>
        </div>
      ) : (
        <div className={styles.gridContainer}>
          <DataGrid items={filteredRows} columns={columns} sortable selectionMode="multiselect"
            selectedItems={selectedItems} onSelectionChange={onSelectionChange}
            getRowId={(item) => item.solutionId} focusMode="composite" size="small" style={{ minWidth: '100%' }}>
            <DataGridHeader>
              <DataGridRow>{({ renderHeaderCell }) => <DataGridHeaderCell>{renderHeaderCell()}</DataGridHeaderCell>}</DataGridRow>
            </DataGridHeader>
            <DataGridBody<SolutionRow>>
              {({ item, rowId }) => (
                <DataGridRow<SolutionRow> key={rowId}
                  onContextMenu={(e: React.MouseEvent) => { e.preventDefault(); setContextMenu({ solutionId: item.solutionId, x: e.clientX, y: e.clientY }); }}>
                  {({ renderCell }) => <DataGridCell>{renderCell(item)}</DataGridCell>}
                </DataGridRow>
              )}
            </DataGridBody>
          </DataGrid>
        </div>
      )}

      {contextMenu && (
        <div style={{ position: 'fixed', left: contextMenu.x, top: contextMenu.y, zIndex: 1000 }}>
          <Menu open onOpenChange={() => setContextMenu(null)}>
            <MenuTrigger><span /></MenuTrigger>
            <MenuPopover><MenuList>
              <MenuItem icon={<OpenRegular />} onClick={() => {
                const envId = auth?.environmentId;
                if (envId) window.open(`https://make.powerapps.com/environments/${envId}/solutions/${contextMenu.solutionId}`, '_blank');
                setContextMenu(null);
              }}>Open in Maker Portal</MenuItem>
            </MenuList></MenuPopover>
          </Menu>
        </div>
      )}
    </div>
  );
}
