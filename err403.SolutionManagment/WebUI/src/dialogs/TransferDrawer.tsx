import { useState } from 'react';
import {
  OverlayDrawer, DrawerHeader, DrawerHeaderTitle, DrawerBody, DrawerFooter,
  Button, Switch, Dropdown, Option, Field, Text, Badge, Input,
  Accordion, AccordionItem, AccordionHeader, AccordionPanel,
  Divider, makeStyles, tokens,
} from '@fluentui/react-components';
import {
  ArrowUploadRegular, DismissRegular,
  SettingsRegular, ArrowDownloadRegular, ArrowUploadFilled,
  CheckmarkCircleRegular, NumberSymbolRegular,
} from '@fluentui/react-icons';
import type { SelectedSolution, TransferSettings, TargetConnection } from '../types';
import { postMessage } from '../bridge';

const useStyles = makeStyles({
  body: {
    display: 'flex',
    flexDirection: 'column',
    gap: '16px',
    padding: '12px 16px',
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: '8px',
  },
  sectionTitle: {
    fontSize: '12px',
    fontWeight: 600,
    color: tokens.colorNeutralForeground3,
    textTransform: 'uppercase' as const,
    letterSpacing: '0.5px',
  },
  solutionList: {
    display: 'flex',
    flexDirection: 'column',
    gap: '4px',
    padding: '8px 10px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: '6px',
    maxHeight: '160px',
    overflow: 'auto',
  },
  solutionItem: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: '8px',
  },
  targetChips: {
    display: 'flex',
    gap: '6px',
    flexWrap: 'wrap',
  },
  settingRow: {
    paddingLeft: '4px',
  },
  footer: {
    display: 'flex',
    justifyContent: 'flex-end',
    gap: '8px',
  },
});

interface TransferDrawerProps {
  solutions: SelectedSolution[];
  targets: TargetConnection[];
  open: boolean;
  onClose: () => void;
}

export function TransferDrawer({ solutions, targets, open, onClose }: TransferDrawerProps) {
  const styles = useStyles();

  const [settings, setSettings] = useState<TransferSettings>({
    managed: true,
    importMode: 'Update',
    overwriteUnmanaged: true,
    publishWorkflows: true,
    checkDependencies: false,
    convertToManaged: false,
  });

  // Extended settings (matching C# Settings.cs)
  const [generalSettings, setGeneralSettings] = useState({
    autoSave: true,
    autoSavePath: '',
    preImportSummary: true,
    refreshInterval: '00:00:10',
    useToastNotifications: true,
  });

  const [exportSettings, setExportSettings] = useState({
    exportAsync: true,
    autoNumbering: false,
    calendarSettings: false,
    customizationSettings: false,
    emailTracking: false,
    externalApps: false,
    generalSettings: false,
    isvConfig: false,
    marketingSettings: false,
    outlookSync: false,
    relationshipRoles: false,
    sales: false,
  });

  const [importSettings, setImportSettings] = useState({
    deployMissingPackages: true,
    skipProductUpdateDeps: false,
  });

  const [publishSettings, setPublishSettings] = useState({
    publishCustomizations: true,
  });

  const [versionSettings, setVersionSettings] = useState({
    updateVersion: 'Prompt' as 'No' | 'Yes' | 'Prompt',
    versionPolicy: 'Date' as 'Major' | 'Minor' | 'Build' | 'Revision' | 'Manual' | 'Date',
    dateVersionMask: 'yyyy.MM.dd.x',
  });

  const handleTransfer = () => {
    postMessage({
      action: 'startTransfer',
      solutions,
      settings,
    });
    onClose();
  };

  const totalOps = solutions.length * targets.length;
  return (
    <OverlayDrawer
      open={open}
      onOpenChange={(_e, data) => { if (!data.open) onClose(); }}
      position="end"
      size="medium"
    >
      <DrawerHeader>
        <DrawerHeaderTitle
          action={<Button appearance="subtle" icon={<DismissRegular />} onClick={onClose} />}
        >
          Transfer Solutions
        </DrawerHeaderTitle>
      </DrawerHeader>

      <DrawerBody className={styles.body}>
        {/* Solutions */}
        <div className={styles.section}>
          <Text className={styles.sectionTitle}>Solutions ({solutions.length})</Text>
          <div className={styles.solutionList}>
            {solutions.map((sol) => (
              <div key={sol.solutionId} className={styles.solutionItem}>
                <Text size={200} weight="semibold">{sol.friendlyName}</Text>
                <Badge size="small" appearance="tint" color="informative">{sol.version}</Badge>
              </div>
            ))}
          </div>
        </div>

        {/* Targets */}
        <div className={styles.section}>
          <Text className={styles.sectionTitle}>Targets ({targets.length})</Text>
          <div className={styles.targetChips}>
            {targets.map((t) => (
              <Badge key={t.name} size="medium" appearance="filled" color="brand">{t.name}</Badge>
            ))}
          </div>
        </div>

        <Divider />

        {/* Settings Accordion */}
        <Accordion multiple defaultOpenItems={['general', 'export', 'import']}>

          {/* General */}
          <AccordionItem value="general">
            <AccordionHeader icon={<SettingsRegular />}>General Settings</AccordionHeader>
            <AccordionPanel className={styles.settingRow}>
              <Switch label="Auto save solutions" checked={generalSettings.autoSave}
                onChange={(_e, d) => setGeneralSettings({ ...generalSettings, autoSave: d.checked })} />
              {generalSettings.autoSave && (
                <Field label="Auto save path" size="small">
                  <Input value={generalSettings.autoSavePath} size="small"
                    onChange={(_e, d) => setGeneralSettings({ ...generalSettings, autoSavePath: d.value })} />
                </Field>
              )}
              <Switch label="Pre Import Summary" checked={generalSettings.preImportSummary}
                onChange={(_e, d) => setGeneralSettings({ ...generalSettings, preImportSummary: d.checked })} />
              <Switch label="Use Windows Toast Notifications" checked={generalSettings.useToastNotifications}
                onChange={(_e, d) => setGeneralSettings({ ...generalSettings, useToastNotifications: d.checked })} />
            </AccordionPanel>
          </AccordionItem>

          {/* Export */}
          <AccordionItem value="export">
            <AccordionHeader icon={<ArrowDownloadRegular />}>Export Settings</AccordionHeader>
            <AccordionPanel className={styles.settingRow}>
              <Switch label="Export as managed" checked={settings.managed}
                onChange={(_e, d) => setSettings({ ...settings, managed: d.checked })} />
              <Switch label="Export asynchronously" checked={exportSettings.exportAsync}
                onChange={(_e, d) => setExportSettings({ ...exportSettings, exportAsync: d.checked })} />
              <Switch label="Autonumber Settings" checked={exportSettings.autoNumbering}
                onChange={(_e, d) => setExportSettings({ ...exportSettings, autoNumbering: d.checked })} />
              <Switch label="Calendar Settings" checked={exportSettings.calendarSettings}
                onChange={(_e, d) => setExportSettings({ ...exportSettings, calendarSettings: d.checked })} />
              <Switch label="Customization Settings" checked={exportSettings.customizationSettings}
                onChange={(_e, d) => setExportSettings({ ...exportSettings, customizationSettings: d.checked })} />
              <Switch label="Email Tracking Settings" checked={exportSettings.emailTracking}
                onChange={(_e, d) => setExportSettings({ ...exportSettings, emailTracking: d.checked })} />
              <Switch label="External Applications" checked={exportSettings.externalApps}
                onChange={(_e, d) => setExportSettings({ ...exportSettings, externalApps: d.checked })} />
              <Switch label="General Settings" checked={exportSettings.generalSettings}
                onChange={(_e, d) => setExportSettings({ ...exportSettings, generalSettings: d.checked })} />
              <Switch label="ISV Config" checked={exportSettings.isvConfig}
                onChange={(_e, d) => setExportSettings({ ...exportSettings, isvConfig: d.checked })} />
              <Switch label="Marketing Settings" checked={exportSettings.marketingSettings}
                onChange={(_e, d) => setExportSettings({ ...exportSettings, marketingSettings: d.checked })} />
              <Switch label="Outlook Synchronization" checked={exportSettings.outlookSync}
                onChange={(_e, d) => setExportSettings({ ...exportSettings, outlookSync: d.checked })} />
              <Switch label="Relationship Roles" checked={exportSettings.relationshipRoles}
                onChange={(_e, d) => setExportSettings({ ...exportSettings, relationshipRoles: d.checked })} />
              <Switch label="Sales" checked={exportSettings.sales}
                onChange={(_e, d) => setExportSettings({ ...exportSettings, sales: d.checked })} />
            </AccordionPanel>
          </AccordionItem>

          {/* Import */}
          <AccordionItem value="import">
            <AccordionHeader icon={<ArrowUploadFilled />}>Import Settings</AccordionHeader>
            <AccordionPanel className={styles.settingRow}>
              <Field label="Import Mode" size="small">
                <Dropdown value={settings.importMode} selectedOptions={[settings.importMode]}
                  onOptionSelect={(_e, d) => setSettings({ ...settings, importMode: (d.optionValue as TransferSettings['importMode']) ?? 'Update' })}
                  size="small">
                  <Option value="Update">Update</Option>
                  <Option value="StageForUpgrade">Stage for Upgrade</Option>
                  <Option value="Upgrade">Upgrade</Option>
                </Dropdown>
              </Field>
              <Switch label="Check for missing dependencies" checked={settings.checkDependencies}
                onChange={(_e, d) => setSettings({ ...settings, checkDependencies: d.checked })} />
              <Switch label="Convert to managed" checked={settings.convertToManaged}
                onChange={(_e, d) => setSettings({ ...settings, convertToManaged: d.checked })} />
              <Switch label="Deploy missing packages" checked={importSettings.deployMissingPackages}
                onChange={(_e, d) => setImportSettings({ ...importSettings, deployMissingPackages: d.checked })} />
              <Switch label="Overwrite unmanaged customizations" checked={settings.overwriteUnmanaged}
                onChange={(_e, d) => setSettings({ ...settings, overwriteUnmanaged: d.checked })} />
              <Switch label="Publish workflows" checked={settings.publishWorkflows}
                onChange={(_e, d) => setSettings({ ...settings, publishWorkflows: d.checked })} />
              <Switch label="Skip product update dependencies" checked={importSettings.skipProductUpdateDeps}
                onChange={(_e, d) => setImportSettings({ ...importSettings, skipProductUpdateDeps: d.checked })} />
            </AccordionPanel>
          </AccordionItem>

          {/* Publish */}
          <AccordionItem value="publish">
            <AccordionHeader icon={<CheckmarkCircleRegular />}>Publish Settings</AccordionHeader>
            <AccordionPanel className={styles.settingRow}>
              <Switch label="Publish Customizations" checked={publishSettings.publishCustomizations}
                onChange={(_e, d) => setPublishSettings({ ...publishSettings, publishCustomizations: d.checked })} />
            </AccordionPanel>
          </AccordionItem>

          {/* Solution Version */}
          <AccordionItem value="version">
            <AccordionHeader icon={<NumberSymbolRegular />}>Solution Version</AccordionHeader>
            <AccordionPanel className={styles.settingRow}>
              <Field label="Update solution version" size="small">
                <Dropdown value={versionSettings.updateVersion} selectedOptions={[versionSettings.updateVersion]}
                  onOptionSelect={(_e, d) => setVersionSettings({ ...versionSettings, updateVersion: (d.optionValue as typeof versionSettings.updateVersion) ?? 'Prompt' })}
                  size="small">
                  <Option value="No">No</Option>
                  <Option value="Yes">Yes</Option>
                  <Option value="Prompt">Prompt</Option>
                </Dropdown>
              </Field>
              <Field label="Version update policy" size="small">
                <Dropdown value={versionSettings.versionPolicy} selectedOptions={[versionSettings.versionPolicy]}
                  onOptionSelect={(_e, d) => setVersionSettings({ ...versionSettings, versionPolicy: (d.optionValue as typeof versionSettings.versionPolicy) ?? 'Date' })}
                  size="small">
                  <Option value="Major">Major (x.0.0.0)</Option>
                  <Option value="Minor">Minor (0.x.0.0)</Option>
                  <Option value="Build">Build (0.0.x.0)</Option>
                  <Option value="Revision">Revision (0.0.0.x)</Option>
                  <Option value="Manual">Manual</Option>
                  <Option value="Date">Date (yyyy.MM.dd.x)</Option>
                </Dropdown>
              </Field>
              {versionSettings.versionPolicy === 'Date' && (
                <Field label="Date Version mask" size="small">
                  <Input value={versionSettings.dateVersionMask} size="small"
                    onChange={(_e, d) => setVersionSettings({ ...versionSettings, dateVersionMask: d.value })} />
                </Field>
              )}
            </AccordionPanel>
          </AccordionItem>

        </Accordion>
      </DrawerBody>

      <DrawerFooter className={styles.footer}>
        <Button appearance="secondary" onClick={onClose}>Cancel</Button>
        <Button appearance="primary" icon={<ArrowUploadRegular />} onClick={handleTransfer}
          disabled={solutions.length === 0 || targets.length === 0}>
          Transfer ({totalOps} operation{totalOps !== 1 ? 's' : ''})
        </Button>
      </DrawerFooter>
    </OverlayDrawer>
  );
}
