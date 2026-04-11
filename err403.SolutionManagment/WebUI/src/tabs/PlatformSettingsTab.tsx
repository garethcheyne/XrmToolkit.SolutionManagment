import { useMemo, useState } from 'react';
import {



  SearchBox, Dropdown, Option, Text, Toolbar, ToolbarButton,
  Badge, Switch, Input, tokens, makeStyles,
  Accordion, AccordionItem, AccordionHeader, AccordionPanel,
} from '@fluentui/react-components';
import { ArrowSyncRegular } from '@fluentui/react-icons';
import { TableSkeleton } from '../components/TableSkeleton';
import { useQuery } from '@tanstack/react-query';
import { getOrgSettings } from '../dataverse';
import { getAuth } from '../auth';

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
  gridContainer: { flex: 1, overflow: 'auto', padding: '0 8px' },
  headerCell: { fontWeight: 700, fontSize: '12px', backgroundColor: tokens.colorNeutralBackground3 },
  boolTrue: { color: tokens.colorPaletteGreenForeground1, fontWeight: 600 },
  boolFalse: { color: tokens.colorPaletteRedForeground1, fontWeight: 600 },
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
  settingRow: {
    display: 'flex', alignItems: 'center', justifyContent: 'space-between',
    padding: '4px 12px', borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
    gap: '12px',
  },
  settingName: { flex: '0 0 45%', minWidth: 0, overflow: 'hidden' },
  settingValue: { flex: '1 1 55%', minWidth: '150px' },
  groupHeader: { fontWeight: 600 },
});

// Categorize org settings by name pattern
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

interface SettingItem {
  key: string;
  value: string;
  type: 'boolean' | 'number' | 'string';
  category: string;
}

interface PlatformSettingsTabProps {
  targets: TargetConnection[];
}

export function PlatformSettingsTab({ targets }: PlatformSettingsTabProps) {
  const styles = useStyles();
  const auth = getAuth();
  const [search, setSearch] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('');

  void targets;

  const { data: orgData, isLoading, refetch } = useQuery({
    queryKey: ['orgSettings', auth?.orgUrl],
    queryFn: getOrgSettings,
    enabled: !!auth,
  });

  const settings: SettingItem[] = useMemo(() => {
    if (!orgData) return [];
    return Object.entries(orgData)
      .filter(([key, value]) => !SKIP_KEYS.has(key) && !key.includes('@') && !key.startsWith('_') && value !== null && value !== undefined && typeof value !== 'object')
      .map(([key, value]) => {
        const strVal = String(value);
        let type: 'boolean' | 'number' | 'string' = 'string';
        if (strVal === 'true' || strVal === 'false' || strVal === 'True' || strVal === 'False') type = 'boolean';
        else if (!isNaN(Number(strVal)) && strVal.trim() !== '') type = 'number';
        return { key, value: strVal, type, category: categorize(key) };
      })
      .sort((a, b) => a.key.localeCompare(b.key));
  }, [orgData]);

  const categories = useMemo(() => [...new Set(settings.map(s => s.category))].sort(), [settings]);

  const filteredSettings = useMemo(() => {
    let result = settings;
    if (categoryFilter) result = result.filter(s => s.category === categoryFilter);
    if (search) {
      const lower = search.toLowerCase();
      result = result.filter(s => s.key.toLowerCase().includes(lower) || s.value.toLowerCase().includes(lower));
    }
    return result;
  }, [settings, categoryFilter, search]);

  // Group by category
  const grouped = useMemo(() => {
    const groups: Record<string, SettingItem[]> = {};
    filteredSettings.forEach(s => {
      if (!groups[s.category]) groups[s.category] = [];
      groups[s.category]?.push(s);
    });
    return groups;
  }, [filteredSettings]);

  if (isLoading) return <TableSkeleton />;

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
          {categories.map(c => <Option key={c} value={c}>{c}</Option>)}
        </Dropdown>
        <Badge className={styles.countBadge} appearance="tint" color="informative" size="medium">
          {filteredSettings.length} setting{filteredSettings.length !== 1 ? 's' : ''}
        </Badge>
      </div>

      {filteredSettings.length === 0 ? (
        <div className={styles.emptyState}>
          <Text size={400} weight="semibold">{!auth ? 'Not connected' : 'No settings found'}</Text>
          <Text size={200}>{!auth ? 'Connect to a source environment first.' : 'Try adjusting your filters.'}</Text>
        </div>
      ) : (
        <div className={styles.gridContainer}>
          <Accordion multiple defaultOpenItems={Object.keys(grouped)}>
            {Object.entries(grouped).map(([category, items]) => (
              <AccordionItem key={category} value={category}>
                <AccordionHeader>
                  <Text className={styles.groupHeader}>{category}</Text>
                  <Badge size="tiny" appearance="tint" color="informative" style={{ marginLeft: 8 }}>{items.length}</Badge>
                </AccordionHeader>
                <AccordionPanel>
                  {items.map(item => (
                    <div key={item.key} className={styles.settingRow}>
                      <div className={styles.settingName}>
                        <Text size={200} weight="semibold" truncate wrap={false} title={item.key}>{item.key}</Text>
                      </div>
                      <div className={styles.settingValue}>
                        {item.type === 'boolean' ? (
                          <Switch
                            checked={item.value.toLowerCase() === 'true'}
                            label={item.value.toLowerCase() === 'true' ? 'True' : 'False'}
                            disabled
                          />
                        ) : item.type === 'number' ? (
                          <Input value={item.value} size="small" readOnly />
                        ) : (
                          <Input value={item.value} size="small" readOnly title={item.value} />
                        )}
                      </div>
                    </div>
                  ))}
                </AccordionPanel>
              </AccordionItem>
            ))}
          </Accordion>
        </div>
      )}
    </div>
  );
}
