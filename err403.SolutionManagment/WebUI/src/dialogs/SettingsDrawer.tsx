
import {
  InlineDrawer, DrawerHeader, DrawerHeaderTitle, DrawerBody,
  Button, Switch, Dropdown, Option, Field, Input,
  Accordion, AccordionItem, AccordionHeader, AccordionPanel,
  makeStyles, tokens,
} from '@fluentui/react-components';
import {
  DismissRegular, SettingsRegular,
  ArrowDownloadRegular, ArrowUploadFilled,
  CheckmarkCircleRegular, NumberSymbolRegular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  drawer: {
    width: '320px',
    minWidth: '320px',
    borderLeft: `1px solid ${tokens.colorNeutralStroke1}`,
    overflowX: 'visible',
    overflowY: 'auto',
  },
  body: {
    display: 'flex',
    flexDirection: 'column',
    gap: '0px',
    overflow: 'visible',
    padding: '12px 16px',
  },
  settingRow: {
    paddingLeft: '0px',
    display: 'flex',
    flexDirection: 'column',
    gap: '0px',
  },
});

// Settings state shared with transfer confirmation
export interface PluginSettings {
  // General
  autoSave: boolean;
  autoSavePath: string;
  preImportSummary: boolean;
  refreshInterval: string;
  useToastNotifications: boolean;
  // Export
  managed: boolean;
  exportAsync: boolean;
  autoNumbering: boolean;
  calendarSettings: boolean;
  customizationSettings: boolean;
  emailTracking: boolean;
  externalApps: boolean;
  generalSettings: boolean;
  isvConfig: boolean;
  marketingSettings: boolean;
  outlookSync: boolean;
  relationshipRoles: boolean;
  sales: boolean;
  // Import
  importMode: 'Update' | 'StageForUpgrade' | 'Upgrade';
  checkDependencies: boolean;
  convertToManaged: boolean;
  deployMissingPackages: boolean;
  overwriteUnmanaged: boolean;
  publishWorkflows: boolean;
  skipProductUpdateDeps: boolean;
  // Publish
  publishCustomizations: boolean;
  // Version
  updateVersion: 'No' | 'Yes' | 'Prompt';
  versionPolicy: 'Major' | 'Minor' | 'Build' | 'Revision' | 'Manual' | 'Date';
  dateVersionMask: string;
}

export const defaultSettings: PluginSettings = {
  autoSave: false,
  autoSavePath: '',
  preImportSummary: true,
  refreshInterval: '00:00:10',
  useToastNotifications: true,
  managed: true,
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
  importMode: 'Update',
  checkDependencies: true,
  convertToManaged: false,
  deployMissingPackages: true,
  overwriteUnmanaged: true,
  publishWorkflows: true,
  skipProductUpdateDeps: false,
  publishCustomizations: true,
  updateVersion: 'Prompt',
  versionPolicy: 'Date',
  dateVersionMask: 'yyyy.MM.dd.x',
};

interface SettingsDrawerProps {
  open: boolean;
  onClose: () => void;
  settings: PluginSettings;
  onSettingsChange: (settings: PluginSettings) => void;
}

export function SettingsDrawer({ open, onClose, settings, onSettingsChange }: SettingsDrawerProps) {
  const styles = useStyles();

  const set = <K extends keyof PluginSettings>(key: K, value: PluginSettings[K]) => {
    onSettingsChange({ ...settings, [key]: value });
  };

  if (!open) return null;

  return (
    <InlineDrawer open={open} position="end" className={styles.drawer}>
      <DrawerHeader>
        <DrawerHeaderTitle
          action={<Button appearance="subtle" icon={<DismissRegular />} onClick={onClose} size="small" />}
        >
          Settings
        </DrawerHeaderTitle>
      </DrawerHeader>

      <DrawerBody className={styles.body}>
        <Accordion multiple defaultOpenItems={["general", "export", "import"]}>

          <AccordionItem value="general">
            <AccordionHeader icon={<SettingsRegular />}>General</AccordionHeader>
            <AccordionPanel className={styles.settingRow}>
              <Switch label="Auto save solutions" checked={settings.autoSave} onChange={(_e, d) => set('autoSave', d.checked)} />
              {settings.autoSave && (
                <Field label="Save path" size="small">
                  <Input value={settings.autoSavePath} size="small" onChange={(_e, d) => set('autoSavePath', d.value)} />
                </Field>
              )}
              <Switch label="Pre-import summary" checked={settings.preImportSummary} onChange={(_e, d) => set('preImportSummary', d.checked)} />
              <Switch label="Toast notifications" checked={settings.useToastNotifications} onChange={(_e, d) => set('useToastNotifications', d.checked)} />
            </AccordionPanel>
          </AccordionItem>

          <AccordionItem value="export">
            <AccordionHeader icon={<ArrowDownloadRegular />} >Export</AccordionHeader>
            <AccordionPanel className={styles.settingRow}>
              <Switch label="Export as managed" checked={settings.managed} onChange={(_e, d) => set('managed', d.checked)} />
              <Switch label="Export async" checked={settings.exportAsync} onChange={(_e, d) => set('exportAsync', d.checked)} />
              <Switch label="Autonumbering" checked={settings.autoNumbering} onChange={(_e, d) => set('autoNumbering', d.checked)} />
              <Switch label="Calendar" checked={settings.calendarSettings} onChange={(_e, d) => set('calendarSettings', d.checked)} />
              <Switch label="Customization" checked={settings.customizationSettings} onChange={(_e, d) => set('customizationSettings', d.checked)} />
              <Switch label="Email Tracking" checked={settings.emailTracking} onChange={(_e, d) => set('emailTracking', d.checked)} />
              <Switch label="External Apps" checked={settings.externalApps} onChange={(_e, d) => set('externalApps', d.checked)} />
              <Switch label="General" checked={settings.generalSettings} onChange={(_e, d) => set('generalSettings', d.checked)} />
              <Switch label="ISV Config" checked={settings.isvConfig} onChange={(_e, d) => set('isvConfig', d.checked)} />
              <Switch label="Marketing" checked={settings.marketingSettings} onChange={(_e, d) => set('marketingSettings', d.checked)} />
              <Switch label="Outlook Sync" checked={settings.outlookSync} onChange={(_e, d) => set('outlookSync', d.checked)} />
              <Switch label="Relationship Roles" checked={settings.relationshipRoles} onChange={(_e, d) => set('relationshipRoles', d.checked)} />
              <Switch label="Sales" checked={settings.sales} onChange={(_e, d) => set('sales', d.checked)} />
            </AccordionPanel>
          </AccordionItem>

          <AccordionItem value="import">
            <AccordionHeader icon={<ArrowUploadFilled />} >Import</AccordionHeader>
            <AccordionPanel className={styles.settingRow}>
              <Field label="Import Mode" size="small">
                <Dropdown value={settings.importMode} selectedOptions={[settings.importMode]}
                  onOptionSelect={(_e, d) => set('importMode', (d.optionValue ?? 'Update') as PluginSettings['importMode'])} size="small">
                  <Option value="Update">Update</Option>
                  <Option value="StageForUpgrade">Stage for Upgrade</Option>
                  <Option value="Upgrade">Upgrade</Option>
                </Dropdown>
              </Field>
              <Switch label="Check dependencies" checked={settings.checkDependencies} onChange={(_e, d) => set('checkDependencies', d.checked)} />
              <Switch label="Convert to managed" checked={settings.convertToManaged} onChange={(_e, d) => set('convertToManaged', d.checked)} />
              <Switch label="Deploy missing packages" checked={settings.deployMissingPackages} onChange={(_e, d) => set('deployMissingPackages', d.checked)} />
              <Switch label="Overwrite unmanaged" checked={settings.overwriteUnmanaged} onChange={(_e, d) => set('overwriteUnmanaged', d.checked)} />
              <Switch label="Publish workflows" checked={settings.publishWorkflows} onChange={(_e, d) => set('publishWorkflows', d.checked)} />
              <Switch label="Skip product update deps" checked={settings.skipProductUpdateDeps} onChange={(_e, d) => set('skipProductUpdateDeps', d.checked)} />
            </AccordionPanel>
          </AccordionItem>

          <AccordionItem value="publish">
            <AccordionHeader icon={<CheckmarkCircleRegular />} >Publish</AccordionHeader>
            <AccordionPanel className={styles.settingRow}>
              <Switch label="Publish customizations" checked={settings.publishCustomizations} onChange={(_e, d) => set('publishCustomizations', d.checked)} />
            </AccordionPanel>
          </AccordionItem>

          <AccordionItem value="version">
            <AccordionHeader icon={<NumberSymbolRegular />} >Version</AccordionHeader>
            <AccordionPanel className={styles.settingRow}>
              <Field label="Update version" size="small">
                <Dropdown value={settings.updateVersion} selectedOptions={[settings.updateVersion]}
                  onOptionSelect={(_e, d) => set('updateVersion', (d.optionValue ?? 'Prompt') as PluginSettings['updateVersion'])} size="small">
                  <Option value="No">No</Option>
                  <Option value="Yes">Yes</Option>
                  <Option value="Prompt">Prompt</Option>
                </Dropdown>
              </Field>
              <Field label="Version policy" size="small">
                <Dropdown value={settings.versionPolicy} selectedOptions={[settings.versionPolicy]}
                  onOptionSelect={(_e, d) => set('versionPolicy', (d.optionValue ?? 'Date') as PluginSettings['versionPolicy'])} size="small">
                  <Option value="Major">Major (x.0.0.0)</Option>
                  <Option value="Minor">Minor (0.x.0.0)</Option>
                  <Option value="Build">Build (0.0.x.0)</Option>
                  <Option value="Revision">Revision (0.0.0.x)</Option>
                  <Option value="Manual">Manual</Option>
                  <Option value="Date">Date (yyyy.MM.dd.x)</Option>
                </Dropdown>
              </Field>
              {settings.versionPolicy === 'Date' && (
                <Field label="Date mask" size="small">
                  <Input value={settings.dateVersionMask} size="small" onChange={(_e, d) => set('dateVersionMask', d.value)} />
                </Field>
              )}
            </AccordionPanel>
          </AccordionItem>

        </Accordion>
      </DrawerBody>
    </InlineDrawer>
  );
}
