import { useCallback, useMemo, useState } from 'react';
import {
  DataGrid, DataGridHeader, DataGridRow, DataGridHeaderCell,
  DataGridBody, DataGridCell, createTableColumn,
  type TableColumnDefinition, type DataGridProps,
  SearchBox, Dropdown, Option, Switch, Text, Toolbar, ToolbarButton, ToolbarDivider,
  Badge, Spinner, Menu, MenuTrigger, MenuPopover, MenuList, MenuItem,
  tokens, makeStyles, type SelectionItemId,
} from '@fluentui/react-components';
import {
  ArrowSyncRegular, PlayRegular, StopRegular, OpenRegular,
} from '@fluentui/react-icons';
import { useQuery } from '@tanstack/react-query';
import { getCloudFlows } from '../dataverse';
import { getAuth } from '../auth';
import { postMessage } from '../bridge';
import { StatusPill } from '../components/StatusPill';
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
  countBadge: { marginLeft: 'auto' },
  emptyState: {
    display: 'flex', flexDirection: 'column', alignItems: 'center',
    justifyContent: 'center', flex: 1, padding: '60px 20px', gap: '8px',
    color: tokens.colorNeutralForeground3,
  },
  loadingState: {
    display: 'flex', alignItems: 'center', justifyContent: 'center', flex: 1, gap: '12px',
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
}

export function CloudFlowsTab({ targets }: CloudFlowsTabProps) {
  const styles = useStyles();
  const auth = getAuth();
  const [search, setSearch] = useState('');
  const [activeOnly, setActiveOnly] = useState(false);
  const [selectedItems, setSelectedItems] = useState<Set<SelectionItemId>>(new Set());
  const [contextMenu, setContextMenu] = useState<{ flowId: string; x: number; y: number } | null>(null);

  void targets; // Will be used for target status comparison

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
    return cols;
  }, []);

  const onSelectionChange: DataGridProps['onSelectionChange'] = useCallback(
    (_e: unknown, data: { selectedItems: Set<SelectionItemId> }) => setSelectedItems(data.selectedItems), []);

  const getSelected = useCallback(() =>
    filteredRows.filter((f) => selectedItems.has(f.workflowId))
      .map((f) => ({ name: f.name, workflowId: f.workflowId, stateCode: f.stateCode })),
    [filteredRows, selectedItems]);

  if (isLoading) {
    return <div className={styles.loadingState}><Spinner size="small" /><Text>Loading cloud flows...</Text></div>;
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
          <DataGrid items={filteredRows} columns={columns} sortable selectionMode="multiselect"
            selectedItems={selectedItems} onSelectionChange={onSelectionChange}
            getRowId={(item) => item.workflowId} focusMode="composite" size="small" style={{ minWidth: '100%' }}>
            <DataGridHeader>
              <DataGridRow>{({ renderHeaderCell }) => <DataGridHeaderCell>{renderHeaderCell()}</DataGridHeaderCell>}</DataGridRow>
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
                if (envId) window.open(`https://make.powerapps.com/environments/${envId}/solutions/fd140aaf-4df4-11dd-bd17-0019b9312238/objects/cloudflows/${contextMenu.flowId}/view`, '_blank');
                setContextMenu(null);
              }}>Open in Power Automate</MenuItem>
            </MenuList></MenuPopover>
          </Menu>
        </div>
      )}
    </div>
  );
}
