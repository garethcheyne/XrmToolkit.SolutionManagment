import { useCallback, useMemo, useState } from 'react';
import {
  DataGrid, DataGridHeader, DataGridRow, DataGridHeaderCell,
  DataGridBody, DataGridCell, createTableColumn,
  type TableColumnDefinition, type DataGridProps,
  SearchBox, Dropdown, Option, Text, Toolbar, ToolbarButton,
  Badge, Spinner, tokens, makeStyles, type SelectionItemId,
} from '@fluentui/react-components';
import { ArrowSyncRegular } from '@fluentui/react-icons';
import { useQuery } from '@tanstack/react-query';
import { getOrgSettings } from '../dataverse';
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
  boolTrue: { color: tokens.colorPaletteGreenForeground1, fontWeight: 600 },
  boolFalse: { color: tokens.colorPaletteRedForeground1, fontWeight: 600 },
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

interface SettingRow {
  uniqueName: string;
  displayName: string;
  value: string;
  category: string;
}

// Skip metadata / navigation properties
const SKIP_KEYS = new Set([
  'organizationid', '@odata.context', '@odata.etag',
  '_createdby_value', '_modifiedby_value', '_createdonbehalfby_value', '_modifiedonbehalfby_value',
]);

function categorize(key: string): string {
  if (key.startsWith('is') || key.startsWith('allow') || key.startsWith('enable') || key.startsWith('block')) return 'Feature Flags';
  if (key.includes('email') || key.includes('mail')) return 'Email';
  if (key.includes('calendar') || key.includes('fiscal')) return 'Calendar';
  if (key.includes('currency') || key.includes('pricing')) return 'Currency';
  if (key.includes('format') || key.includes('locale') || key.includes('language')) return 'Localization';
  return 'General';
}

interface PlatformSettingsTabProps {
  targets: TargetConnection[];
}

export function PlatformSettingsTab({ targets }: PlatformSettingsTabProps) {
  const styles = useStyles();
  const auth = getAuth();
  const [search, setSearch] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('');
  const [selectedItems, setSelectedItems] = useState<Set<SelectionItemId>>(new Set());

  void targets;

  const { data: orgData, isLoading, refetch } = useQuery({
    queryKey: ['orgSettings', auth?.orgUrl],
    queryFn: getOrgSettings,
    enabled: !!auth,
  });

  const rows: SettingRow[] = useMemo(() => {
    if (!orgData) return [];
    return Object.entries(orgData)
      .filter(([key, value]) => !SKIP_KEYS.has(key) && value !== null && value !== undefined && typeof value !== 'object')
      .map(([key, value]) => ({
        uniqueName: key,
        displayName: key,
        value: String(value),
        category: categorize(key),
      }))
      .sort((a, b) => a.displayName.localeCompare(b.displayName));
  }, [orgData]);

  const categories = useMemo(
    () => [...new Set(rows.map((s) => s.category))].sort(),
    [rows]
  );

  const filteredRows = useMemo(() => {
    let result = rows;
    if (categoryFilter) result = result.filter((s) => s.category === categoryFilter);
    if (search) {
      const lower = search.toLowerCase();
      result = result.filter((s) =>
        s.displayName.toLowerCase().includes(lower) ||
        s.value.toLowerCase().includes(lower)
      );
    }
    return result;
  }, [rows, categoryFilter, search]);

  const columns = useMemo((): TableColumnDefinition<SettingRow>[] => [
    createTableColumn({ columnId: 'displayName', compare: (a, b) => a.displayName.localeCompare(b.displayName),
      renderHeaderCell: () => 'Setting Name',
      renderCell: (item) => <Text truncate wrap={false} title={item.displayName} weight="semibold">{item.displayName}</Text>,
    }),
    createTableColumn({ columnId: 'category', compare: (a, b) => a.category.localeCompare(b.category),
      renderHeaderCell: () => 'Category',
      renderCell: (item) => <Badge appearance="tint" color="informative" size="small">{item.category}</Badge>,
    }),
    createTableColumn({ columnId: 'value', compare: (a, b) => a.value.localeCompare(b.value),
      renderHeaderCell: () => 'Value',
      renderCell: (item) => {
        if (item.value === 'True') return <Text className={styles.boolTrue}>True</Text>;
        if (item.value === 'False') return <Text className={styles.boolFalse}>False</Text>;
        return <Text truncate wrap={false} title={item.value}>{item.value}</Text>;
      },
    }),
  ], [styles]);

  const onSelectionChange: DataGridProps['onSelectionChange'] = useCallback(
    (_e: unknown, data: { selectedItems: Set<SelectionItemId> }) => setSelectedItems(data.selectedItems), []);

  if (isLoading) {
    return <div className={styles.loadingState}><Spinner size="small" /><Text>Loading organization settings...</Text></div>;
  }

  return (
    <div className={styles.root}>
      <Toolbar className={styles.toolbar} size="small">
        <ToolbarButton icon={<ArrowSyncRegular />} onClick={() => refetch()}>Refresh</ToolbarButton>
      </Toolbar>

      <div className={styles.searchRow}>
        <SearchBox className={styles.searchBox} placeholder="Search settings..." value={search}
          onChange={(_e, data) => setSearch(data.value)} />
        <Text size={200} weight="semibold">Category:</Text>
        <Dropdown placeholder="(All)" value={categoryFilter || '(All)'}
          selectedOptions={categoryFilter ? [categoryFilter] : []}
          onOptionSelect={(_e, data) => setCategoryFilter(data.optionValue === '(All)' ? '' : (data.optionValue ?? ''))}
          style={{ minWidth: 160 }} size="small">
          <Option value="(All)">(All)</Option>
          {categories.map((c) => <Option key={c} value={c}>{c}</Option>)}
        </Dropdown>
        <Badge className={styles.countBadge} appearance="tint" color="informative" size="medium">
          {filteredRows.length} setting{filteredRows.length !== 1 ? 's' : ''}
          {selectedItems.size > 0 ? ` (${selectedItems.size} selected)` : ''}
        </Badge>
      </div>

      {filteredRows.length === 0 ? (
        <div className={styles.emptyState}>
          <Text size={400} weight="semibold">{!auth ? 'Not connected' : 'No settings found'}</Text>
          <Text size={200}>{!auth ? 'Connect to a source environment first.' : 'Try adjusting your filters.'}</Text>
        </div>
      ) : (
        <div className={styles.gridContainer}>
          <DataGrid items={filteredRows} columns={columns} sortable selectionMode="multiselect"
            selectedItems={selectedItems} onSelectionChange={onSelectionChange}
            getRowId={(item) => item.uniqueName} focusMode="composite" size="small" style={{ minWidth: '100%' }}>
            <DataGridHeader>
              <DataGridRow>{({ renderHeaderCell }) => <DataGridHeaderCell>{renderHeaderCell()}</DataGridHeaderCell>}</DataGridRow>
            </DataGridHeader>
            <DataGridBody<SettingRow>>
              {({ item, rowId }) => (
                <DataGridRow<SettingRow> key={rowId}>
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
