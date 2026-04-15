import { useState, useMemo, useEffect } from 'react';
import {
  Dialog, DialogSurface, DialogTitle, DialogBody,
  DialogContent, DialogActions,
  Button, Text, Badge, Divider, Switch, Dropdown, Option,
  Field, Checkbox, Link,
  DataGrid, DataGridHeader, DataGridHeaderCell, DataGridBody, DataGridRow, DataGridCell,
  createTableColumn,
  makeStyles, tokens,
  type TableColumnDefinition,
  TeachingPopover, TeachingPopoverTrigger, TeachingPopoverSurface,
  TeachingPopoverHeader, TeachingPopoverBody, TeachingPopoverFooter,
  Popover, PopoverTrigger, PopoverSurface,
} from '@fluentui/react-components';
import {
  ArrowUploadRegular, InfoRegular,
} from '@fluentui/react-icons';
import type { SelectedSolution, TargetConnection, TransferSettings } from '../types';
import type { PluginSettings } from '../panels/SettingsPanel';
import { getEffectiveSettings } from '../panels/SettingsPanel';
import { postMessage } from '../bridge';
import { bumpVersion } from '../utils/versionUtils';

const useStyles = makeStyles({
  surface: {
    maxWidth: '860px',
    width: '860px',
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: '12px',
  },
  subtitle: {
    fontSize: '12px',
    color: tokens.colorNeutralForeground3,
  },
  sectionTitle: {
    fontSize: '11px',
    fontWeight: 600,
    color: tokens.colorNeutralForeground3,
    textTransform: 'uppercase' as const,
    letterSpacing: '0.5px',
  },
  settingsGrid: {
    display: 'grid',
    gridTemplateColumns: '1fr auto',
    gap: '6px 16px',
    padding: '8px 0',
    alignItems: 'center',
  },
  targetChips: {
    display: 'flex',
    gap: '4px',
    flexWrap: 'wrap',
  },
  versionSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: '4px',
    paddingTop: '4px',
  },
  versionNote: {
    fontSize: '11px',
    color: tokens.colorPaletteRedForeground1,
    fontStyle: 'italic',
  },
  totalOps: {
    display: 'flex',
    justifyContent: 'flex-end',
    gap: '4px',
    fontSize: '12px',
    color: tokens.colorNeutralForeground3,
  },
});

interface VersionRow {
  solutionId: string;
  friendlyName: string;
  uniqueName: string;
  currentVersion: string;
  newVersion: string;
}

interface TransferConfirmDialogProps {
  solutions: SelectedSolution[];
  targets: TargetConnection[];
  settings: PluginSettings;
  open: boolean;
  onClose: () => void;
}

export function TransferConfirmDialog({ solutions, targets, settings: pluginSettings, open, onClose }: TransferConfirmDialogProps) {
  const styles = useStyles();

  // Local editable copies of settings for this transfer
  const [managed, setManaged] = useState(pluginSettings.managed);
  const [checkDeps, setCheckDeps] = useState(pluginSettings.checkDependencies);
  const [convertToManaged, setConvertToManaged] = useState(pluginSettings.convertToManaged);
  const [overwriteUnmanaged, setOverwriteUnmanaged] = useState(pluginSettings.overwriteUnmanaged);
  const [skipProductDeps, setSkipProductDeps] = useState(pluginSettings.skipProductUpdateDeps);
  const [importMode, setImportMode] = useState(pluginSettings.importMode);
  const [skipVersionUpdate, setSkipVersionUpdate] = useState(false);
  const [versionChecked, setVersionChecked] = useState<Set<string>>(() => new Set(solutions.map((s) => s.solutionId)));

  // Reset local state when dialog opens so it always reflects current settings
  useEffect(() => {
    if (open) {
      setManaged(pluginSettings.managed);
      setCheckDeps(pluginSettings.checkDependencies);
      setConvertToManaged(pluginSettings.convertToManaged);
      setOverwriteUnmanaged(pluginSettings.overwriteUnmanaged);
      setSkipProductDeps(pluginSettings.skipProductUpdateDeps);
      setImportMode(pluginSettings.importMode);
      setSkipVersionUpdate(false);
      setVersionChecked(new Set(solutions.map((s) => s.solutionId)));
    }
  }, [open, pluginSettings, solutions]);

  const versionRows: VersionRow[] = useMemo(() =>
    solutions.map((s) => {
      const eff = getEffectiveSettings(pluginSettings, s.uniqueName);
      return {
        solutionId: s.solutionId,
        friendlyName: s.friendlyName,
        uniqueName: s.uniqueName,
        currentVersion: s.version,
        newVersion: bumpVersion(s.version, eff.versionPolicy, eff.dateVersionMask),
      };
    }),
    [solutions, pluginSettings]
  );

  const versionColumns: TableColumnDefinition<VersionRow>[] = useMemo(() => [
    createTableColumn({
      columnId: 'friendlyName',
      renderHeaderCell: () => 'Friendly name',
      renderCell: (item) => <Text size={200} weight="semibold">{item.friendlyName}</Text>,
    }),
    createTableColumn({
      columnId: 'uniqueName',
      renderHeaderCell: () => 'Unique name',
      renderCell: (item) => <Text size={200}>{item.uniqueName}</Text>,
    }),
    createTableColumn({
      columnId: 'currentVersion',
      renderHeaderCell: () => 'Current version',
      renderCell: (item) => <Badge size="small" appearance="tint" color="informative">{item.currentVersion}</Badge>,
    }),
    createTableColumn({
      columnId: 'newVersion',
      renderHeaderCell: () => 'New version',
      renderCell: (item) => {
        const willUpdate = !skipVersionUpdate && versionChecked.has(item.solutionId);
        return willUpdate
          ? <Badge size="small" appearance="tint" color="success">{item.newVersion}</Badge>
          : <Text size={200} style={{ color: tokens.colorNeutralForeground4 }}>—</Text>;
      },
    }),
  ], [skipVersionUpdate, versionChecked]);

  const showVersionTable = pluginSettings.updateVersion !== 'No';

  const totalOps = solutions.length * targets.length;

  const handleConfirm = () => {
    const profiles = pluginSettings.solutionProfiles ?? {};
    const perSolution: Record<string, Omit<TransferSettings, 'perSolution'>> = {};
    for (const sol of solutions) {
      const eff = getEffectiveSettings(pluginSettings, sol.uniqueName);
      if (profiles[sol.uniqueName]) {
        perSolution[sol.uniqueName] = {
          managed: eff.managed,
          importMode: eff.importMode,
          overwriteUnmanaged: eff.overwriteUnmanaged,
          publishWorkflows: eff.publishWorkflows,
          checkDependencies: eff.checkDependencies,
          convertToManaged: eff.convertToManaged,
          skipProductUpdateDeps: eff.skipProductUpdateDeps,
          autoNumbering: eff.autoNumbering,
          calendarSettings: eff.calendarSettings,
          customizationSettings: eff.customizationSettings,
          emailTracking: eff.emailTracking,
          externalApps: eff.externalApps,
          generalSettings: eff.generalSettings,
          isvConfig: eff.isvConfig,
          marketingSettings: eff.marketingSettings,
          outlookSync: eff.outlookSync,
          relationshipRoles: eff.relationshipRoles,
          sales: eff.sales,
        };
      }
    }

    // Build solutions list with version override info
    const solsWithVersion = solutions.map((s) => {
      const row = versionRows.find((r) => r.solutionId === s.solutionId);
      const willUpdate = showVersionTable && !skipVersionUpdate && versionChecked.has(s.solutionId);
      return { ...s, newVersion: willUpdate ? row?.newVersion : undefined };
    });

    postMessage({
      action: 'startTransfer',
      solutions: solsWithVersion,
      settings: {
        managed,
        importMode,
        overwriteUnmanaged,
        publishWorkflows: pluginSettings.publishWorkflows,
        checkDependencies: checkDeps,
        convertToManaged,
        skipProductUpdateDeps: skipProductDeps,
        autoNumbering: pluginSettings.autoNumbering,
        calendarSettings: pluginSettings.calendarSettings,
        customizationSettings: pluginSettings.customizationSettings,
        emailTracking: pluginSettings.emailTracking,
        externalApps: pluginSettings.externalApps,
        generalSettings: pluginSettings.generalSettings,
        isvConfig: pluginSettings.isvConfig,
        marketingSettings: pluginSettings.marketingSettings,
        outlookSync: pluginSettings.outlookSync,
        relationshipRoles: pluginSettings.relationshipRoles,
        sales: pluginSettings.sales,
        ...(Object.keys(perSolution).length > 0 ? { perSolution } : {}),
      },
    });
    onClose();
  };

  return (
    <Dialog open={open} onOpenChange={(_e, data) => { if (!data.open) onClose(); }}>
      <DialogSurface className={styles.surface}>
        <DialogBody>
          <DialogTitle>Pre Import Summary</DialogTitle>
          <DialogContent className={styles.content}>
            <Text className={styles.subtitle}>
              This is a summary of the settings used to transfer selected solutions. You can change some settings now.
            </Text>

            {/* Editable import settings */}
            <div className={styles.settingsGrid}>
              <Text size={300}>Import as managed</Text>
              <Switch checked={managed} onChange={(_e, d) => setManaged(d.checked)}
                label={managed ? 'True' : 'False'} />

              <Text size={300} style={{ display: 'inline-flex', alignItems: 'center', gap: '4px' }}>
                Check for missing dependencies
                <Popover withArrow size="small">
                  <PopoverTrigger>
                    <Button appearance="subtle" size="small" icon={<InfoRegular fontSize={13} />}
                      style={{ minWidth: 0, padding: '0 2px', height: '18px', color: tokens.colorNeutralForeground3 }}
                      aria-label="About check dependencies" />
                  </PopoverTrigger>
                  <PopoverSurface style={{ maxWidth: '260px' }}>
                    <Text size={100} style={{ display: 'block', marginBottom: '4px' }}>
                      Checks each target environment for missing required components before the import starts.
                      Warnings appear in the progress panel.
                    </Text>
                    <Link href="https://learn.microsoft.com/en-us/power-platform/alm/dependency-tracking-solution-components" target="_blank" style={{ fontSize: '11px' }}>Dependency tracking</Link>
                  </PopoverSurface>
                </Popover>
              </Text>
              <Switch checked={checkDeps} onChange={(_e, d) => setCheckDeps(d.checked)}
                label={checkDeps ? 'True' : 'False'} />

              <Text size={300}>Convert to managed</Text>
              <Switch checked={convertToManaged} onChange={(_e, d) => setConvertToManaged(d.checked)}
                label={convertToManaged ? 'True' : 'False'} />

              <Text size={300} style={{ display: 'inline-flex', alignItems: 'center', gap: '4px' }}>
                Overwrite unmanaged customizations
                <Popover withArrow size="small">
                  <PopoverTrigger>
                    <Button appearance="subtle" size="small" icon={<InfoRegular fontSize={13} />}
                      style={{ minWidth: 0, padding: '0 2px', height: '18px', color: tokens.colorPaletteYellowForeground2 }}
                      aria-label="About overwrite unmanaged" />
                  </PopoverTrigger>
                  <PopoverSurface style={{ maxWidth: '260px' }}>
                    <Text size={100} style={{ display: 'block', marginBottom: '4px' }}>
                      ⚠ Overwrites any manual customisations made directly on the target environment.
                      Leave on unless the target has bespoke changes you need to preserve.
                    </Text>
                    <Link href="https://learn.microsoft.com/en-us/power-apps/maker/data-platform/update-solutions#overwrite-customizations-option" target="_blank" style={{ fontSize: '11px' }}>Overwrite customisations option</Link>
                  </PopoverSurface>
                </Popover>
              </Text>
              <Switch checked={overwriteUnmanaged} onChange={(_e, d) => setOverwriteUnmanaged(d.checked)}
                label={overwriteUnmanaged ? 'True' : 'False'} />

              <Text size={300}>Skip product update dependencies</Text>
              <Switch checked={skipProductDeps} onChange={(_e, d) => setSkipProductDeps(d.checked)}
                label={skipProductDeps ? 'True' : 'False'} />

              <Text size={300} style={{ display: 'inline-flex', alignItems: 'center', gap: '4px' }}>
                Import mode
                <TeachingPopover>
                  <TeachingPopoverTrigger>
                    <Button appearance="subtle" size="small" icon={<InfoRegular fontSize={13} />}
                      style={{ minWidth: 0, padding: '0 2px', height: '18px', color: tokens.colorBrandForeground1 }}
                      aria-label="Import mode help" />
                  </TeachingPopoverTrigger>
                  <TeachingPopoverSurface style={{ maxWidth: '340px' }}>
                    <TeachingPopoverHeader>Choose the right import mode</TeachingPopoverHeader>
                    <TeachingPopoverBody>
                      <Text size={200} style={{ display: 'block', marginBottom: '6px' }}>
                        <strong>Update</strong> — Fastest. Replaces existing components. Unused components from the previous version stay on target.
                      </Text>
                      <Text size={200} style={{ display: 'block', marginBottom: '6px' }}>
                        <strong>Upgrade</strong> — Removes components no longer in the solution. Use for a clean state.
                      </Text>
                      <Text size={200}>
                        <strong>Stage for Upgrade</strong> — Stages new version alongside old. Lets you migrate data before completing the upgrade.
                      </Text>
                    </TeachingPopoverBody>
                    <TeachingPopoverFooter
                      primary={
                        <a href="https://learn.microsoft.com/en-us/power-apps/maker/data-platform/update-solutions"
                          target="_blank" rel="noreferrer"
                          style={{ color: 'inherit', textDecoration: 'underline' }}
                        >Upgrade or update a solution ↗</a>
                      }
                    />
                  </TeachingPopoverSurface>
                </TeachingPopover>
              </Text>
              <Field size="small">
                <Dropdown value={importMode} selectedOptions={[importMode]}
                  onOptionSelect={(_e, d) => setImportMode((d.optionValue ?? 'Update') as typeof importMode)}
                  size="small" style={{ minWidth: '160px' }}>
                  <Option value="Update">Update (keep unused components)</Option>
                  <Option value="Upgrade">Upgrade (remove unused components)</Option>
                  <Option value="StageForUpgrade">Stage for Upgrade (deferred)</Option>
                </Dropdown>
              </Field>
            </div>

            <Divider />

            {/* Targets */}
            <Text className={styles.sectionTitle}>Targets ({targets.length})</Text>
            <div className={styles.targetChips}>
              {targets.map((t) => (
                <Badge key={t.name} size="medium" appearance="filled" color="brand">{t.name}</Badge>
              ))}
            </div>

            {/* Version table */}
            {showVersionTable && (
              <>
                <Divider />
                <div className={styles.versionSection}>
                  <DataGrid
                    items={versionRows}
                    columns={versionColumns}
                    selectionMode="multiselect"
                    selectedItems={versionChecked}
                    onSelectionChange={(_e, data) => setVersionChecked(data.selectedItems as Set<string>)}
                    getRowId={(item) => item.solutionId}
                    size="small"
                    style={{ minWidth: '100%' }}
                  >
                    <DataGridHeader>
                      <DataGridRow>
                        {({ renderHeaderCell }) => <DataGridHeaderCell>{renderHeaderCell()}</DataGridHeaderCell>}
                      </DataGridRow>
                    </DataGridHeader>
                    <DataGridBody<VersionRow>>
                      {({ item, rowId }) => (
                        <DataGridRow<VersionRow> key={rowId}>
                          {({ renderCell }) => <DataGridCell>{renderCell(item)}</DataGridCell>}
                        </DataGridRow>
                      )}
                    </DataGridBody>
                  </DataGrid>

                  <div style={{ display: 'flex', alignItems: 'center', gap: '16px', paddingTop: '4px' }}>
                    <Checkbox
                      label="Skip new solution version"
                      checked={skipVersionUpdate}
                      onChange={(_e, d) => setSkipVersionUpdate(!!d.checked)}
                    />
                    <Text className={styles.versionNote}>
                      Only checked solutions will have their version updated
                    </Text>
                  </div>
                </div>
              </>
            )}

            <div className={styles.totalOps}>
              Total operations: <strong>{totalOps}</strong>
            </div>

          </DialogContent>
          <DialogActions>
            <Button appearance="secondary" onClick={onClose}>Cancel</Button>
            <Button appearance="primary" icon={<ArrowUploadRegular />} onClick={handleConfirm}>
              Transfer
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
