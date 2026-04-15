import { useCallback, useMemo, useState } from 'react';
import {
  DataGrid, DataGridHeader, DataGridRow, DataGridHeaderCell,
  DataGridBody, DataGridCell, createTableColumn,
  type TableColumnDefinition, type DataGridProps,
  SearchBox, Dropdown, Option, Text, Toolbar, ToolbarButton, ToolbarDivider,
  Badge, Switch, tokens, makeStyles, type SelectionItemId,
} from '@fluentui/react-components';
import { ArrowSyncRegular, ArrowUploadRegular } from '@fluentui/react-icons';
import { TableSkeleton } from '../components/TableSkeleton';
import { useQuery } from '@tanstack/react-query';
import { getOrgSettings, getSettingDefinitions } from '../dataverse';
import { getAuth } from '../auth';
import { postMessage } from '../bridge';
import { ConfirmDialog, emptyConfirm, type ConfirmDialogState } from '../dialogs/ConfirmDialog';
import { Allotment } from 'allotment';
import 'allotment/dist/style.css';
import { PlatformSettingPanel } from '../panels/PlatformSettingPanel';

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
  gridContainer: { height: '100%', overflow: 'auto' },
  headerCell: { fontWeight: 700, fontSize: '12px', backgroundColor: tokens.colorNeutralBackground3 },
  matchCell: { color: tokens.colorPaletteGreenForeground1 },
  mismatchCell: { color: tokens.colorPaletteRedForeground1, fontWeight: 600 },
  notFoundCell: { color: tokens.colorNeutralForeground4, fontStyle: 'italic' },
  countBadge: { marginLeft: 'auto' },
  emptyState: {
    display: 'flex', flexDirection: 'column', alignItems: 'center',
    justifyContent: 'center', flex: 1, padding: '60px 20px', gap: '8px',
    color: tokens.colorNeutralForeground3,
  },
});

// Fallback categorization when definition metadata is unavailable
function categorize(key: string): string {
  if (key.startsWith('is') || key.startsWith('allow') || key.startsWith('enable') || key.startsWith('block') || key.startsWith('require')) return 'Features';
  if (key.includes('email') || key.includes('mail')) return 'Email';
  if (key.includes('calendar') || key.includes('fiscal') || key.includes('date') || key.includes('time')) return 'Calendar & Time';
  if (key.includes('currency') || key.includes('pricing')) return 'Currency';
  if (key.includes('format') || key.includes('locale') || key.includes('language') || key.includes('numberseparator')) return 'Localization';
  if (key.includes('max') || key.includes('min') || key.includes('limit') || key.includes('threshold')) return 'Limits';
  if (key.includes('plugin') || key.includes('trace') || key.includes('debug') || key.includes('log')) return 'Diagnostics';
  if (key.includes('sharepoint') || key.includes('onenote') || key.includes('teams') || key.includes('yammer')) return 'Integration';
  return 'General';
}

const SKIP_KEYS = new Set([
  'organizationid', '@odata.context', '@odata.etag',
  '_createdby_value', '_modifiedby_value', '_createdonbehalfby_value', '_modifiedonbehalfby_value',
]);

export interface SettingRow {
  key: string;
  displayName: string;
  description: string;
  value: string;
  defaultValue: string;
  type: 'boolean' | 'number' | 'string';
  category: string;
}

interface PlatformSettingsTabProps {
  targets: TargetConnection[];
  targetOrgSettingsData: Record<string, Record<string, unknown>>;
}

export function PlatformSettingsTab({ targets, targetOrgSettingsData }: PlatformSettingsTabProps) {
  const styles = useStyles();
  const auth = getAuth();
  const [search, setSearch] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('');
  const [diffsOnly, setDiffsOnly] = useState(false);
  const [showSchema, setShowSchema] = useState(false);
  const [selectedItems, setSelectedItems] = useState<Set<SelectionItemId>>(new Set());
  const [confirm, setConfirm] = useState<ConfirmDialogState>(emptyConfirm);
  const [editSetting, setEditSetting] = useState<SettingRow | null>(null);

  const { data: orgData, isLoading, refetch } = useQuery({
    queryKey: ['orgSettings', auth?.orgUrl],
    queryFn: getOrgSettings,
    enabled: !!auth,
  });

  const { data: definitions = [] } = useQuery({
    queryKey: ['settingDefinitions', auth?.orgUrl],
    queryFn: getSettingDefinitions,
    enabled: !!auth,
  });

  // Build a lookup from schema name to definition metadata
  const defLookup = useMemo(() => {
    const map = new Map<string, { displayname: string; description: string; groupname: string; defaultvalue: string }>();
    for (const d of definitions) {
      map.set(d.uniquename?.toLowerCase(), d);
    }
    return map;
  }, [definitions]);

  const settings: SettingRow[] = useMemo(() => {
    if (!orgData) return [];
    return Object.entries(orgData)
      .filter(([key, value]) => !SKIP_KEYS.has(key) && !key.includes('@') && !key.startsWith('_') && value !== null && value !== undefined && typeof value !== 'object')
      .map(([key, value]) => {
        const strVal = String(value);
        let type: 'boolean' | 'number' | 'string' = 'string';
        if (strVal === 'true' || strVal === 'false' || strVal === 'True' || strVal === 'False') type = 'boolean';
        else if (!isNaN(Number(strVal)) && strVal.trim() !== '') type = 'number';
        const def = defLookup.get(key.toLowerCase());
        return {
          key,
          displayName: def?.displayname || key,
          description: def?.description || '',
          value: strVal,
          defaultValue: def?.defaultvalue || '',
          type,
          category: def?.groupname || categorize(key),
        };
      })
      .sort((a, b) => a.displayName.localeCompare(b.displayName));
  }, [orgData, defLookup]);

  const categories = useMemo(() => [...new Set(settings.map(s => s.category))].sort(), [settings]);

  // Check if a setting differs from any target
  const hasDiff = useCallback((item: SettingRow): boolean => {
    return targets.some(t => {
      const tData = targetOrgSettingsData[t.name];
      if (!tData) return false;
      const tVal = tData[item.key];
      return tVal !== undefined && String(tVal) !== item.value;
    });
  }, [targets, targetOrgSettingsData]);

  const filteredSettings = useMemo(() => {
    let result = settings;
    if (categoryFilter) result = result.filter(s => s.category === categoryFilter);
    if (search) {
      const lower = search.toLowerCase();
      result = result.filter(s => s.key.toLowerCase().includes(lower) || s.displayName.toLowerCase().includes(lower) || s.value.toLowerCase().includes(lower));
    }
    if (diffsOnly && targets.length > 0) {
      result = result.filter(hasDiff);
    }
    return result;
  }, [settings, categoryFilter, search, diffsOnly, targets.length, hasDiff]);

  const columns = useMemo(() => {
    const cols: TableColumnDefinition<SettingRow>[] = [
      createTableColumn({
        columnId: 'category',
        compare: (a, b) => a.category.localeCompare(b.category),
        renderHeaderCell: () => 'Category',
        renderCell: (item) => <Badge appearance="tint" color="informative" size="small">{item.category}</Badge>,
      }),
      createTableColumn({
        columnId: 'key',
        compare: (a, b) => (showSchema ? a.key.localeCompare(b.key) : a.displayName.localeCompare(b.displayName)),
        renderHeaderCell: () => 'Setting',
        renderCell: (item) => {
          const label = showSchema ? item.key : item.displayName;
          return <Text truncate wrap={false} title={`${item.displayName}\n${item.key}${item.description ? '\n' + item.description : ''}`} weight="semibold" size={200}>{label}</Text>;
        },
      }),
      createTableColumn({
        columnId: 'value',
        compare: (a, b) => a.value.localeCompare(b.value),
        renderHeaderCell: () => 'Source Value',
        renderCell: (item) => <Text truncate wrap={false} title={item.value} size={200}>{item.value}</Text>,
      }),
    ];

    // Add target columns
    for (const t of targets) {
      const tData = targetOrgSettingsData[t.name];
      cols.push(createTableColumn({
        columnId: `target_${t.name}`,
        compare: (a, b) => {
          const va = tData ? String(tData[a.key] ?? '') : '';
          const vb = tData ? String(tData[b.key] ?? '') : '';
          return va.localeCompare(vb);
        },
        renderHeaderCell: () => t.name,
        renderCell: (item) => {
          if (!tData) return <Text className={styles.notFoundCell} size={200}>—</Text>;
          const tVal = tData[item.key];
          if (tVal === undefined || tVal === null) return <Text className={styles.notFoundCell} size={200}>—</Text>;
          const tStr = String(tVal);
          const isMatch = tStr === item.value;
          return <Text className={isMatch ? styles.matchCell : styles.mismatchCell} truncate wrap={false} size={200}
            title={tStr}>{tStr}</Text>;
        },
      }));
    }

    return cols;
  }, [showSchema, targets, targetOrgSettingsData, styles]);

  const onSelectionChange: DataGridProps['onSelectionChange'] = useCallback(
    (_e: unknown, data: { selectedItems: Set<SelectionItemId> }) => setSelectedItems(data.selectedItems), []);

  const handleSyncSelected = () => {
    const selected = filteredSettings.filter(r => selectedItems.has(r.key));
    if (selected.length === 0) return;
    setConfirm({
      open: true,
      title: 'Sync Selected Settings',
      message: `Sync ${selected.length} setting(s) to ${targets.length} target(s)?`,
      severity: 'warning',
      confirmLabel: 'Sync',
      onConfirm: () => postMessage({
        action: 'syncSettings',
        items: selected.map(r => ({ uniqueName: r.key, displayName: r.key, sourceValue: r.value })),
        all: false,
      }),
    });
  };

  const handleSyncAllDiffs = () => {
    const diffs = filteredSettings.filter(hasDiff);
    if (diffs.length === 0) return;
    setConfirm({
      open: true,
      title: 'Sync All Differences',
      message: `Sync ${diffs.length} differing setting(s) to ${targets.length} target(s)?`,
      severity: 'warning',
      confirmLabel: 'Sync All',
      onConfirm: () => postMessage({
        action: 'syncSettings',
        items: diffs.map(r => ({ uniqueName: r.key, displayName: r.key, sourceValue: r.value })),
        all: true,
      }),
    });
  };

  if (isLoading) return <TableSkeleton />;

  return (
    <div className={styles.root}>
      <Toolbar className={styles.toolbar} size="small">
        <ToolbarButton icon={<ArrowSyncRegular />} onClick={() => refetch()}>Refresh</ToolbarButton>
        {targets.length > 0 && (
          <>
            <ToolbarDivider />
            <ToolbarButton icon={<ArrowUploadRegular />} disabled={selectedItems.size === 0}
              onClick={handleSyncSelected}>Sync Selected</ToolbarButton>
            <ToolbarButton onClick={handleSyncAllDiffs}>Sync All Diffs</ToolbarButton>
          </>
        )}
      </Toolbar>

      <div className={styles.searchRow}>
        <SearchBox className={styles.searchBox} placeholder="Search settings..." value={search}
          onChange={(_e, data) => setSearch(data.value)} />
        <Switch label="Schema names" checked={showSchema} onChange={(_e, data) => setShowSchema(data.checked)} />
        <Text size={200} weight="semibold">Category:</Text>
        <Dropdown placeholder="(All)" value={categoryFilter || '(All)'}
          selectedOptions={categoryFilter ? [categoryFilter] : []}
          onOptionSelect={(_e, data) => setCategoryFilter(data.optionValue === '(All)' ? '' : (data.optionValue ?? ''))}
          style={{ minWidth: 160 }} size="small">
          <Option value="(All)">(All)</Option>
          {categories.map(c => <Option key={c} value={c}>{c}</Option>)}
        </Dropdown>
        {targets.length > 0 && (
          <Switch label="Diffs only" checked={diffsOnly} onChange={(_e, data) => setDiffsOnly(data.checked)} />
        )}
        <Badge className={styles.countBadge} appearance="tint" color="informative" size="medium">
          {filteredSettings.length} setting{filteredSettings.length !== 1 ? 's' : ''}
          {selectedItems.size > 0 ? ` (${selectedItems.size} selected)` : ''}
        </Badge>
      </div>

      <Allotment proportionalLayout={false}>
        <Allotment.Pane>
          {filteredSettings.length === 0 ? (
            <div className={styles.emptyState}>
              <Text size={400} weight="semibold">{!auth ? 'Not connected' : 'No settings found'}</Text>
              <Text size={200}>{!auth ? 'Connect to a source environment first.' : 'Try adjusting your filters.'}</Text>
            </div>
          ) : (
            <div className={styles.gridContainer}>
              <DataGrid items={filteredSettings} columns={columns} sortable resizableColumns
                selectionMode="multiselect"
                selectedItems={selectedItems} onSelectionChange={onSelectionChange}
                getRowId={(item) => item.key} focusMode="composite" size="small" style={{ minWidth: '100%' }}>
                <DataGridHeader style={{ position: 'sticky', top: 0, zIndex: 1, backgroundColor: tokens.colorNeutralBackground3 }}>
                  <DataGridRow>{({ renderHeaderCell }) => <DataGridHeaderCell className={styles.headerCell}>{renderHeaderCell()}</DataGridHeaderCell>}</DataGridRow>
                </DataGridHeader>
                <DataGridBody<SettingRow>>
                  {({ item, rowId }) => (
                    <DataGridRow<SettingRow> key={rowId} onClick={() => setEditSetting(item)}>
                      {({ renderCell }) => <DataGridCell>{renderCell(item)}</DataGridCell>}
                    </DataGridRow>
                  )}
                </DataGridBody>
              </DataGrid>
            </div>
          )}
        </Allotment.Pane>
        {editSetting && (
          <Allotment.Pane preferredSize={350} minSize={280} maxSize={500}>
            <PlatformSettingPanel
              setting={editSetting}
              targets={targets}
              targetOrgSettingsData={targetOrgSettingsData}
              onClose={() => setEditSetting(null)}
            />
          </Allotment.Pane>
        )}
      </Allotment>

      <ConfirmDialog state={confirm} onClose={() => setConfirm(emptyConfirm)} />
    </div>
  );
}
