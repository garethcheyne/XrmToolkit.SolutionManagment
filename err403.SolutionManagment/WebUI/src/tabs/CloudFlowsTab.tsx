import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  DataGrid, DataGridHeader, DataGridRow, DataGridHeaderCell,
  DataGridBody, DataGridCell, createTableColumn,
  type TableColumnDefinition, type DataGridProps,
  SearchBox, Dropdown, Option, Switch, Text, Toolbar, ToolbarButton, ToolbarDivider,
  Badge, Menu, MenuTrigger, MenuPopover, MenuList, MenuItem, Tooltip,
  tokens, makeStyles, type SelectionItemId,
} from '@fluentui/react-components';
import {
  ArrowSyncRegular, PlayRegular, StopRegular, OpenRegular,
} from '@fluentui/react-icons';
import { TableSkeleton } from '../components/TableSkeleton';
import { useQuery } from '@tanstack/react-query';
import { getCloudFlows } from '../dataverse';
import { getAuth } from '../auth';
import { postMessage, setBridgeHandler } from '../bridge';
import { StatusPill } from '../components/StatusPill';
import { FlowResultsDialog } from '../dialogs/FlowResultsDialog';
import type { TargetConnection } from '../types';

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
  gridContainer: { flex: 1, overflow: 'auto' },
  headerCell: { fontWeight: 700, fontSize: '12px', backgroundColor: tokens.colorNeutralBackground3 },
  countBadge: { marginLeft: 'auto' },
  emptyState: {
    display: 'flex', flexDirection: 'column', alignItems: 'center',
    justifyContent: 'center', flex: 1, padding: '60px 20px', gap: '8px',
    color: tokens.colorNeutralForeground3,
  },
  loadingState: {
    display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '100%', gap: '12px',
  },
});

function getStatusText(stateCode: number): string {
  switch (stateCode) {
    case 0: return 'Off';
    case 1: return 'On';
    case 2: return 'Suspended';
    default: return `Unknown (${stateCode})`;
  }
}

function getCategoryText(category: number): string {
  switch (category) {
    case 0: return 'Classic';
    case 5: return 'Cloud Flow';
    case 6: return 'Desktop Flow';
    default: return `Other (${category})`;
  }
}

interface FlowRow {
  workflowId: string;
  name: string;
  type: string;
  status: string;
  stateCode: number;
  owner: string;
  modifiedOn: string;
}

interface CloudFlowsTabProps {
  targets: TargetConnection[];
  targetFlowData: Record<string, Array<{ name: string; statecode: number; statuscode: number }>>;
}

export function CloudFlowsTab({ targets, targetFlowData }: CloudFlowsTabProps) {
  const styles = useStyles();
  const auth = getAuth();
  const [search, setSearch] = useState('');
  const [activeOnly, setActiveOnly] = useState(false);
  const [selectedItems, setSelectedItems] = useState<Set<SelectionItemId>>(new Set());
  const [contextMenu, setContextMenu] = useState<{ flowId: string; x: number; y: number } | null>(null);
  const [flowResults, setFlowResults] = useState<Array<{ FlowName: string; TargetName: string; Success: boolean; ErrorMessage: string }>>([]);
  const [resultsOpen, setResultsOpen] = useState(false);

  useEffect(() => {
    setBridgeHandler('flowResults', (json: string) => {
      const results = JSON.parse(json);
      setFlowResults(results);
      setResultsOpen(true);
    });
  }, []);

  const { data: workflows = [], isLoading, refetch } = useQuery({
    queryKey: ['cloudFlows', auth?.orgUrl],
    queryFn: getCloudFlows,
    enabled: !!auth,
  });

  const rows: FlowRow[] = useMemo(() =>
    workflows.map((wf) => ({
      workflowId: wf.workflowid,
      name: wf.name ?? '(unnamed)',
      type: getCategoryText(wf.category),
      status: getStatusText(wf.statecode),
      stateCode: wf.statecode,
      owner: wf['ownerid@OData.Community.Display.V1.FormattedValue'] ?? '',
      modifiedOn: wf.modifiedon ? new Date(wf.modifiedon).toLocaleDateString('en-GB', { year: '2-digit', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' }) : '',
    })),
    [workflows]
  );

  const filteredRows = useMemo(() => {
    let result = rows;
    if (activeOnly) result = result.filter((f) => f.stateCode === 1);
    if (search) {
      const lower = search.toLowerCase();
      result = result.filter((f) =>
        f.name.toLowerCase().includes(lower) ||
        f.owner.toLowerCase().includes(lower)
      );
    }
    return result;
  }, [rows, activeOnly, search]);

  const columns = useMemo(() => {
    const cols: TableColumnDefinition<FlowRow>[] = [
      createTableColumn({ columnId: 'name', compare: (a, b) => a.name.localeCompare(b.name),
        renderHeaderCell: () => 'Flow Name',
        renderCell: (item) => <Text truncate wrap={false} title={item.name} weight="regular">{item.name}</Text>,
      }),
      createTableColumn({ columnId: 'type', compare: (a, b) => a.type.localeCompare(b.type),
        renderHeaderCell: () => 'Type', renderCell: (item) => item.type,
      }),
      createTableColumn({ columnId: 'status', compare: (a, b) => a.stateCode - b.stateCode,
        renderHeaderCell: () => 'Status',
        renderCell: (item) => <StatusPill status={item.status} stateCode={item.stateCode} />,
      }),
      createTableColumn({ columnId: 'owner', compare: (a, b) => a.owner.localeCompare(b.owner),
        renderHeaderCell: () => 'Owner', renderCell: (item) => item.owner,
      }),
      createTableColumn({ columnId: 'modifiedOn', compare: (a, b) => a.modifiedOn.localeCompare(b.modifiedOn),
        renderHeaderCell: () => 'Modified', renderCell: (item) => item.modifiedOn,
      }),
    ];

    // Target status columns
    for (const t of targets) {
      const tFlows = targetFlowData[t.name];
      cols.push(createTableColumn({
        columnId: `target_${t.name}`,
        compare: (a, b) => {
          const sa = tFlows?.find(f => f.name === a.name)?.statecode ?? -1;
          const sb = tFlows?.find(f => f.name === b.name)?.statecode ?? -1;
          return sa - sb;
        },
        renderHeaderCell: () => t.name,
        renderCell: (item) => {
          if (!tFlows) return <Text size={200} style={{ color: tokens.colorNeutralForeground4 }}>—</Text>;
          const match = tFlows.find(f => f.name === item.name);
          if (!match) return <Text size={200} style={{ color: tokens.colorNeutralForeground4 }}>not found</Text>;
          const isOn = match.statecode === 1;
          return (
            <Tooltip content={`Click to ${isOn ? 'deactivate' : 'activate'} on ${t.name}`} relationship="label">
              <Switch
                checked={isOn}
                onChange={() => {
                  postMessage({
                    action: isOn ? 'deactivateFlows' : 'activateFlows',
                    flows: [{ name: item.name, workflowId: item.workflowId, stateCode: item.stateCode }],
                  });
                }}
                label={isOn ? 'On' : 'Off'}
              />
            </Tooltip>
          );
        },
      }));
    }

    return cols;
  }, [targets, targetFlowData]);

  const onSelectionChange: DataGridProps['onSelectionChange'] = useCallback(
    (_e: unknown, data: { selectedItems: Set<SelectionItemId> }) => setSelectedItems(data.selectedItems), []);

  const getSelected = useCallback(() =>
    filteredRows.filter((f) => selectedItems.has(f.workflowId))
      .map((f) => ({ name: f.name, workflowId: f.workflowId, stateCode: f.stateCode })),
    [filteredRows, selectedItems]);

  if (isLoading) {
    return <TableSkeleton />;
  }

  return (
    <div className={styles.root}>
      <Toolbar className={styles.toolbar} size="small">
        <ToolbarButton icon={<ArrowSyncRegular />} onClick={() => refetch()}>Refresh</ToolbarButton>
        <ToolbarDivider />
        <ToolbarButton icon={<PlayRegular />} onClick={() => { const s = getSelected(); if (s.length) postMessage({ action: 'activateFlows', flows: s }); }}>Activate</ToolbarButton>
        <ToolbarButton icon={<StopRegular />} onClick={() => { const s = getSelected(); if (s.length) postMessage({ action: 'deactivateFlows', flows: s }); }}>Deactivate</ToolbarButton>
      </Toolbar>

      <div className={styles.searchRow}>
        <SearchBox className={styles.searchBox} placeholder="Search flows..." value={search}
          onChange={(_e, data) => setSearch(data.value)} />
        <Text size={200} weight="semibold">Solution:</Text>
        <Dropdown placeholder="(All)" style={{ minWidth: 180 }} size="small">
          <Option value="(All)">(All)</Option>
        </Dropdown>
        <Switch label="Active only" checked={activeOnly} onChange={(_e, data) => setActiveOnly(data.checked)} />
        <Badge className={styles.countBadge} appearance="tint" color="informative" size="medium">
          {filteredRows.length} flow{filteredRows.length !== 1 ? 's' : ''}
          {selectedItems.size > 0 ? ` (${selectedItems.size} selected)` : ''}
        </Badge>
      </div>

      {filteredRows.length === 0 ? (
        <div className={styles.emptyState}>
          <Text size={400} weight="semibold">{!auth ? 'Not connected' : 'No cloud flows found'}</Text>
          <Text size={200}>{!auth ? 'Connect to a source environment first.' : 'Try adjusting your filters.'}</Text>
        </div>
      ) : (
        <div className={styles.gridContainer}>
          <DataGrid items={filteredRows} columns={columns} sortable resizableColumns selectionMode="multiselect"
            selectedItems={selectedItems} onSelectionChange={onSelectionChange}
            getRowId={(item) => item.workflowId} focusMode="composite" size="small" style={{ minWidth: '100%' }}>
            <DataGridHeader style={{ position: 'sticky', top: 0, zIndex: 1, backgroundColor: tokens.colorNeutralBackground3 }}>
              <DataGridRow>{({ renderHeaderCell }) => <DataGridHeaderCell className={styles.headerCell}>{renderHeaderCell()}</DataGridHeaderCell>}</DataGridRow>
            </DataGridHeader>
            <DataGridBody<FlowRow>>
              {({ item, rowId }) => (
                <DataGridRow<FlowRow> key={rowId}
                  onContextMenu={(e: React.MouseEvent) => { e.preventDefault(); setContextMenu({ flowId: item.workflowId, x: e.clientX, y: e.clientY }); }}>
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
                if (envId) postMessage({ action: 'openUrl', url: `https://make.powerapps.com/environments/${envId}/solutions/fd140aaf-4df4-11dd-bd17-0019b9312238/objects/cloudflows/${contextMenu.flowId}/view` });
                setContextMenu(null);
              }}>Open in Power Automate</MenuItem>
            </MenuList></MenuPopover>
          </Menu>
        </div>
      )}

      <FlowResultsDialog
        results={flowResults.map(r => ({ flowName: r.FlowName, targetName: r.TargetName, success: r.Success, errorMessage: r.ErrorMessage, FlowName: r.FlowName, TargetName: r.TargetName, Success: r.Success, ErrorMessage: r.ErrorMessage, IsConnectionRefError: false }))}
        open={resultsOpen}
        onClose={() => setResultsOpen(false)}
      />
    </div>
  );
}
