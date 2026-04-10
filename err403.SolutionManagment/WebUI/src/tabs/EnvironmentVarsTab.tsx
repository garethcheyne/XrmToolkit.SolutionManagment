import { useCallback, useMemo, useState } from 'react';
import {
  DataGrid, DataGridHeader, DataGridRow, DataGridHeaderCell,
  DataGridBody, DataGridCell, createTableColumn,
  type TableColumnDefinition, type DataGridProps,
  SearchBox, Text, Toolbar, ToolbarButton, ToolbarDivider,
  Badge, Switch, Spinner, tokens, makeStyles, type SelectionItemId,
} from '@fluentui/react-components';
import { ArrowSyncRegular, ArrowUploadRegular } from '@fluentui/react-icons';
import { useQuery } from '@tanstack/react-query';
import { getEnvVarDefinitions, getEnvVarValues } from '../dataverse';
import { getAuth } from '../auth';
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
  matchCell: { color: tokens.colorPaletteGreenForeground1 },
  mismatchCell: { color: tokens.colorPaletteRedForeground1 },
  notFoundCell: { color: tokens.colorNeutralForeground4, fontStyle: 'italic' },
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

function getTypeName(type: number): string {
  switch (type) {
    case 100000000: return 'String';
    case 100000001: return 'Number';
    case 100000002: return 'Boolean';
    case 100000003: return 'JSON';
    case 100000004: return 'Data Source';
    case 100000005: return 'Secret';
    default: return 'String';
  }
}

interface EnvVarRow {
  definitionId: string;
  displayName: string;
  schemaName: string;
  type: string;
  defaultValue: string;
  currentValue: string;
  targetValues: Record<string, { value: string; exists: boolean }>;
}

interface EnvironmentVarsTabProps {
  targets: TargetConnection[];
}

export function EnvironmentVarsTab({ targets }: EnvironmentVarsTabProps) {
  const styles = useStyles();
  const auth = getAuth();
  const [search, setSearch] = useState('');
  const [showSchema, setShowSchema] = useState(false);
  const [selectedItems, setSelectedItems] = useState<Set<SelectionItemId>>(new Set());

  const { data: definitions = [], isLoading: loadingDefs, refetch: refetchDefs } = useQuery({
    queryKey: ['envVarDefs', auth?.orgUrl],
    queryFn: getEnvVarDefinitions,
    enabled: !!auth,
  });

  const { data: values = [], isLoading: loadingVals, refetch: refetchVals } = useQuery({
    queryKey: ['envVarVals', auth?.orgUrl],
    queryFn: getEnvVarValues,
    enabled: !!auth,
  });

  const rows: EnvVarRow[] = useMemo(() =>
    definitions.map((def) => {
      const val = values.find((v) => v._environmentvariabledefinitionid_value === def.environmentvariabledefinitionid);
      return {
        definitionId: def.environmentvariabledefinitionid,
        displayName: def.displayname ?? '',
        schemaName: def.schemaname ?? '',
        type: getTypeName(def.type),
        defaultValue: def.defaultvalue ?? '',
        currentValue: val?.value ?? '',
        targetValues: {},
      };
    }),
    [definitions, values]
  );

  // Target queries would follow same pattern as SolutionsTab
  // For now, target comparison is handled in the columns

  const filteredRows = useMemo(() => {
    if (!search) return rows;
    const lower = search.toLowerCase();
    return rows.filter((ev) =>
      ev.displayName.toLowerCase().includes(lower) ||
      ev.schemaName.toLowerCase().includes(lower) ||
      ev.type.toLowerCase().includes(lower)
    );
  }, [rows, search]);

  const columns = useMemo(() => {
    const cols: TableColumnDefinition<EnvVarRow>[] = [
      createTableColumn({ columnId: 'displayName', compare: (a, b) => a.displayName.localeCompare(b.displayName),
        renderHeaderCell: () => 'Display Name',
        renderCell: (item) => <Text truncate wrap={false} title={item.displayName} weight="semibold">{item.displayName}</Text>,
      }),
    ];

    if (showSchema) {
      cols.push(createTableColumn({ columnId: 'schemaName', compare: (a, b) => a.schemaName.localeCompare(b.schemaName),
        renderHeaderCell: () => 'Schema Name',
        renderCell: (item) => <Text truncate wrap={false} size={200}>{item.schemaName}</Text>,
      }));
    }

    cols.push(
      createTableColumn({ columnId: 'type', compare: (a, b) => a.type.localeCompare(b.type),
        renderHeaderCell: () => 'Type',
        renderCell: (item) => <Badge appearance="tint" color="informative" size="small">{item.type}</Badge>,
      }),
      createTableColumn({ columnId: 'defaultValue', compare: (a, b) => a.defaultValue.localeCompare(b.defaultValue),
        renderHeaderCell: () => 'Default',
        renderCell: (item) => <Text truncate wrap={false} size={200} title={item.defaultValue}>{item.defaultValue || '—'}</Text>,
      }),
      createTableColumn({ columnId: 'currentValue', compare: (a, b) => a.currentValue.localeCompare(b.currentValue),
        renderHeaderCell: () => 'Current Value',
        renderCell: (item) => <Text truncate wrap={false} title={item.currentValue}>{item.currentValue || '(default)'}</Text>,
      }),
    );

    // Target columns would be added here when target env var fetching is implemented
    void targets; // acknowledge prop usage

    return cols;
  }, [showSchema, targets]);

  const onSelectionChange: DataGridProps['onSelectionChange'] = useCallback(
    (_e: unknown, data: { selectedItems: Set<SelectionItemId> }) => setSelectedItems(data.selectedItems), []);

  const isLoading = loadingDefs || loadingVals;

  if (isLoading) {
    return <div className={styles.loadingState}><Spinner size="small" /><Text>Loading environment variables...</Text></div>;
  }

  return (
    <div className={styles.root}>
      <Toolbar className={styles.toolbar} size="small">
        <ToolbarButton icon={<ArrowSyncRegular />} onClick={() => { refetchDefs(); refetchVals(); }}>Refresh</ToolbarButton>
        <ToolbarDivider />
        <ToolbarButton icon={<ArrowUploadRegular />} disabled={selectedItems.size === 0}>Transfer Selected</ToolbarButton>
      </Toolbar>

      <div className={styles.searchRow}>
        <SearchBox className={styles.searchBox} placeholder="Search variables..." value={search}
          onChange={(_e, data) => setSearch(data.value)} />
        <Switch label="Schema names" checked={showSchema} onChange={(_e, data) => setShowSchema(data.checked)} />
        <Badge className={styles.countBadge} appearance="tint" color="informative" size="medium">
          {filteredRows.length} variable{filteredRows.length !== 1 ? 's' : ''}
          {selectedItems.size > 0 ? ` (${selectedItems.size} selected)` : ''}
        </Badge>
      </div>

      {filteredRows.length === 0 ? (
        <div className={styles.emptyState}>
          <Text size={400} weight="semibold">{!auth ? 'Not connected' : 'No environment variables found'}</Text>
          <Text size={200}>{!auth ? 'Connect to a source environment first.' : 'Try adjusting your search.'}</Text>
        </div>
      ) : (
        <div className={styles.gridContainer}>
          <DataGrid items={filteredRows} columns={columns} sortable selectionMode="multiselect"
            selectedItems={selectedItems} onSelectionChange={onSelectionChange}
            getRowId={(item) => item.definitionId} focusMode="composite" size="small" style={{ minWidth: '100%' }}>
            <DataGridHeader>
              <DataGridRow>{({ renderHeaderCell }) => <DataGridHeaderCell>{renderHeaderCell()}</DataGridHeaderCell>}</DataGridRow>
            </DataGridHeader>
            <DataGridBody<EnvVarRow>>
              {({ item, rowId }) => (
                <DataGridRow<EnvVarRow> key={rowId}>
                  {({ renderCell }) => <DataGridCell>{renderCell(item)}</DataGridCell>}
                </DataGridRow>
              )}
            </DataGridBody>
          </DataGrid>
        </div>
      )}
    </div>
  );
}
