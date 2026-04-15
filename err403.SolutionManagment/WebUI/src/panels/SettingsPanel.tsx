
import {
  Button, Switch, Dropdown, Option, Field, Input,
  Accordion, AccordionItem, AccordionHeader, AccordionPanel,
  makeStyles, tokens, Text, Divider,
  Popover, PopoverTrigger, PopoverSurface,
  TeachingPopover, TeachingPopoverTrigger, TeachingPopoverSurface,
  TeachingPopoverHeader, TeachingPopoverBody, TeachingPopoverFooter,
  Link,
} from '@fluentui/react-components';
import {
  SettingsRegular,
  ArrowDownloadRegular, ArrowUploadFilled,
  CheckmarkCircleRegular, NumberSymbolRegular,
  AddRegular, DeleteRegular, InfoRegular,
} from '@fluentui/react-icons';
import { useState } from 'react';
import { Panel } from '../components/Panel';

const useStyles = makeStyles({
  settingRow: {
    display: 'flex',
    flexDirection: 'column',
    gap: '2px',
    paddingBottom: '4px',
  },
  profileSelector: {
    display: 'flex',
    alignItems: 'center',
    gap: '6px',
    padding: '4px 0',
  },
  profileLabel: {
    fontSize: '11px',
    fontWeight: 600,
    color: tokens.colorNeutralForeground3,
    textTransform: 'uppercase' as const,
    letterSpacing: '0.5px',
  },
});

// Per-solution profile settings (export/import/publish/version only)
export interface SolutionProfileSettings {
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
  importMode: 'Update' | 'StageForUpgrade' | 'Upgrade';
  checkDependencies: boolean;
  convertToManaged: boolean;
  deployMissingPackages: boolean;
  overwriteUnmanaged: boolean;
  publishWorkflows: boolean;
  skipProductUpdateDeps: boolean;
  publishCustomizations: boolean;
  updateVersion: 'No' | 'Yes' | 'Prompt';
  versionPolicy: 'Major' | 'Minor' | 'Build' | 'Revision' | 'Manual' | 'Date';
  dateVersionMask: string;
}

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
  // Per-solution profiles (key = solution unique name)
  solutionProfiles: Record<string, SolutionProfileSettings>;
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
  solutionProfiles: {},
};

/** Get effective settings for a specific solution (profile overrides defaults). */
export function getEffectiveSettings(settings: PluginSettings, solutionUniqueName?: string): PluginSettings {
  if (!solutionUniqueName || !settings.solutionProfiles[solutionUniqueName]) return settings;
  return { ...settings, ...settings.solutionProfiles[solutionUniqueName] };
}

/** Extract the profile-able fields from current settings. */
function extractProfile(s: PluginSettings): SolutionProfileSettings {
  return {
    managed: s.managed, exportAsync: s.exportAsync,
    autoNumbering: s.autoNumbering, calendarSettings: s.calendarSettings,
    customizationSettings: s.customizationSettings, emailTracking: s.emailTracking,
    externalApps: s.externalApps, generalSettings: s.generalSettings,
    isvConfig: s.isvConfig, marketingSettings: s.marketingSettings,
    outlookSync: s.outlookSync, relationshipRoles: s.relationshipRoles,
    sales: s.sales, importMode: s.importMode,
    checkDependencies: s.checkDependencies, convertToManaged: s.convertToManaged,
    deployMissingPackages: s.deployMissingPackages, overwriteUnmanaged: s.overwriteUnmanaged,
    publishWorkflows: s.publishWorkflows, skipProductUpdateDeps: s.skipProductUpdateDeps,
    publishCustomizations: s.publishCustomizations, updateVersion: s.updateVersion,
    versionPolicy: s.versionPolicy, dateVersionMask: s.dateVersionMask,
  };
}

interface SettingsPanelProps {
  open: boolean;
  onClose: () => void;
  settings: PluginSettings;
  onSettingsChange: (settings: PluginSettings) => void;
  /** List of solution unique names available for profile creation */
  solutionNames?: string[];
  /** Currently selected solution (pre-selects its profile) */
  selectedSolution?: string;
}

const DEFAULT_PROFILE = '__default__';

/** Small information popover for inline setting help. */
function InfoPopover({ body, learnHref, learnLabel }: { body: string; learnHref?: string; learnLabel?: string }) {
  return (
    <Popover withArrow size="small">
      <PopoverTrigger>
        <Button appearance="subtle" size="small" icon={<InfoRegular fontSize={14} />}
          style={{ minWidth: 0, padding: '0 2px', height: '20px', color: tokens.colorNeutralForeground3 }}
          aria-label="More information" />
      </PopoverTrigger>
      <PopoverSurface style={{ maxWidth: '280px' }}>
        <Text size={100} style={{ display: 'block', marginBottom: learnHref ? '6px' : 0 }}>{body}</Text>
        {learnHref && (
          <Link href={learnHref} target="_blank" style={{ fontSize: '11px' }}>
            {learnLabel ?? 'MS Learn'}
          </Link>
        )}
      </PopoverSurface>
    </Popover>
  );
}

/** Label with an inline info popover. */
function LabelWithHelp({ label, body, learnHref, learnLabel }: { label: string; body: string; learnHref?: string; learnLabel?: string }) {
  return (
    <span style={{ display: 'inline-flex', alignItems: 'center', gap: '2px' }}>
      {label}
      <InfoPopover body={body} learnHref={learnHref} learnLabel={learnLabel} />
    </span>
  );
}

export function SettingsPanel({ open, onClose, settings, onSettingsChange, selectedSolution }: SettingsPanelProps) {
  const styles = useStyles();
  const [activeProfile, setActiveProfile] = useState<string>(DEFAULT_PROFILE);

  // Defensive: ensure solutionProfiles is always an object
  const profiles = settings.solutionProfiles ?? {};

  // When panel opens with a selected solution that has a profile, show it
  const currentProfile = activeProfile !== DEFAULT_PROFILE && activeProfile
    ? activeProfile
    : selectedSolution && profiles[selectedSolution]
      ? selectedSolution
      : DEFAULT_PROFILE;

  const isDefault = currentProfile === DEFAULT_PROFILE;

  // Get the effective settings to display
  const displaySettings = isDefault
    ? settings
    : { ...settings, ...(profiles[currentProfile] ?? {}) };

  const set = <K extends keyof PluginSettings>(key: K, value: PluginSettings[K]) => {
    if (isDefault) {
      onSettingsChange({ ...settings, [key]: value });
    } else {
      // Update the per-solution profile
      const profile = { ...extractProfile({ ...settings, ...(profiles[currentProfile] ?? {}) }), [key]: value };
      onSettingsChange({
        ...settings,
        solutionProfiles: { ...profiles, [currentProfile]: profile },
      });
    }
  };

  const handleCreateProfile = () => {
    if (!selectedSolution || profiles[selectedSolution]) return;
    const profile = extractProfile(settings); // Clone current defaults
    onSettingsChange({
      ...settings,
      solutionProfiles: { ...profiles, [selectedSolution]: profile },
    });
    setActiveProfile(selectedSolution);
  };

  const handleDeleteProfile = (name: string) => {
    const { [name]: _, ...rest } = profiles;
    onSettingsChange({ ...settings, solutionProfiles: rest });
    setActiveProfile(DEFAULT_PROFILE);
  };

  if (!open) return null;

  return (
    <Panel title="Settings" onClose={onClose}>
        {/* Profile selector */}
        <div className={styles.profileSelector}>
          <Text className={styles.profileLabel}>Profile</Text>
          <Dropdown
            size="small"
            value={isDefault ? 'Default' : currentProfile}
            selectedOptions={[currentProfile]}
            onOptionSelect={(_e, d) => setActiveProfile(d.optionValue ?? DEFAULT_PROFILE)}
            style={{ flex: 1, minWidth: 0 }}
          >
            <Option value={DEFAULT_PROFILE}>Default (all solutions)</Option>
            {Object.keys(profiles).map((name) => (
              <Option key={name} value={name}>{name}</Option>
            ))}
          </Dropdown>
          {selectedSolution && !profiles[selectedSolution] && (
            <Button size="small" icon={<AddRegular />} appearance="subtle"
              title={`Create profile for ${selectedSolution}`}
              onClick={handleCreateProfile} />
          )}
          {!isDefault && (
            <Button size="small" icon={<DeleteRegular />} appearance="subtle"
              title="Delete this profile"
              onClick={() => handleDeleteProfile(currentProfile)} />
          )}
        </div>
        <Divider />

        <Accordion multiple defaultOpenItems={["general", "export", "import"]}>

          {isDefault && (
            <AccordionItem value="general">
              <AccordionHeader icon={<SettingsRegular />}>General</AccordionHeader>
              <AccordionPanel className={styles.settingRow}>
                <Switch label="Auto save solutions" checked={settings.autoSave} onChange={(_e, d) => set('autoSave', d.checked)} />
                {settings.autoSave && (
                  <Field label="Save path" size="small">
                    <Input value={settings.autoSavePath} size="small" onChange={(_e, d) => set('autoSavePath', d.value)} />
                  </Field>
                )}
                <Field label="Refresh interval" size="small">
                  <Input value={settings.refreshInterval} size="small" placeholder="00:00:10" onChange={(_e, d) => set('refreshInterval', d.value)} />
                </Field>
                <Switch label="Pre-import summary" checked={settings.preImportSummary} onChange={(_e, d) => set('preImportSummary', d.checked)} />
                <Switch label="Toast notifications" checked={settings.useToastNotifications} onChange={(_e, d) => set('useToastNotifications', d.checked)} />
              </AccordionPanel>
            </AccordionItem>
          )}

          <AccordionItem value="export">
            <AccordionHeader icon={<ArrowDownloadRegular />} >Export</AccordionHeader>
            <AccordionPanel className={styles.settingRow}>
              <Switch label="Export as managed" checked={displaySettings.managed} onChange={(_e, d) => set('managed', d.checked)} />
              <Text size={100} style={{ color: tokens.colorNeutralForeground3, marginTop: '-6px', marginBottom: '4px', marginLeft: '40px' }}>
                Managed solutions are deployed to test/UAT/production environments.
                {' '}
                <Link href="https://learn.microsoft.com/en-us/power-platform/alm/solution-concepts-alm#managed-and-unmanaged-solutions" target="_blank" style={{ fontSize: '11px' }}>
                  Learn more
                </Link>
              </Text>
              <Switch label="Export async" checked={displaySettings.exportAsync} onChange={(_e, d) => set('exportAsync', d.checked)} />
              <Switch label="Autonumbering" checked={displaySettings.autoNumbering} onChange={(_e, d) => set('autoNumbering', d.checked)} />
              <Switch label="Calendar" checked={displaySettings.calendarSettings} onChange={(_e, d) => set('calendarSettings', d.checked)} />
              <Switch label="Customization" checked={displaySettings.customizationSettings} onChange={(_e, d) => set('customizationSettings', d.checked)} />
              <Switch label="Email Tracking" checked={displaySettings.emailTracking} onChange={(_e, d) => set('emailTracking', d.checked)} />
              <Switch label="External Apps" checked={displaySettings.externalApps} onChange={(_e, d) => set('externalApps', d.checked)} />
              <Switch label="General" checked={displaySettings.generalSettings} onChange={(_e, d) => set('generalSettings', d.checked)} />
              <Switch label="ISV Config" checked={displaySettings.isvConfig} onChange={(_e, d) => set('isvConfig', d.checked)} />
              <Switch label="Marketing" checked={displaySettings.marketingSettings} onChange={(_e, d) => set('marketingSettings', d.checked)} />
              <Switch label="Outlook Sync" checked={displaySettings.outlookSync} onChange={(_e, d) => set('outlookSync', d.checked)} />
              <Switch label="Relationship Roles" checked={displaySettings.relationshipRoles} onChange={(_e, d) => set('relationshipRoles', d.checked)} />
              <Switch label="Sales" checked={displaySettings.sales} onChange={(_e, d) => set('sales', d.checked)} />
            </AccordionPanel>
          </AccordionItem>

          <AccordionItem value="import">
            <AccordionHeader icon={<ArrowUploadFilled />} >Import</AccordionHeader>
            <AccordionPanel className={styles.settingRow}>
              <Field
                label={
                  <span style={{ display: 'inline-flex', alignItems: 'center', gap: '4px' }}>
                    Import Mode
                    <TeachingPopover>
                      <TeachingPopoverTrigger>
                        <Button appearance="subtle" size="small" icon={<InfoRegular fontSize={14} />}
                          style={{ minWidth: 0, padding: '0 2px', height: '20px', color: tokens.colorBrandForeground1 }}
                          aria-label="Import mode help" />
                      </TeachingPopoverTrigger>
                      <TeachingPopoverSurface style={{ maxWidth: '340px' }}>
                        <TeachingPopoverHeader>Choose the right import mode</TeachingPopoverHeader>
                        <TeachingPopoverBody>
                          <Text size={200} style={{ display: 'block', marginBottom: '8px' }}>
                            <strong>Update</strong> — Fastest option. Replaces existing components. Unused
                            components from the previous version are NOT removed. Recommended for regular deployments.
                          </Text>
                          <Text size={200} style={{ display: 'block', marginBottom: '8px' }}>
                            <strong>Upgrade</strong> — Merges all patches, removes components no longer in the
                            solution. Use when you want a clean state on the target.
                          </Text>
                          <Text size={200}>
                            <strong>Stage for Upgrade</strong> — Stages the new version alongside the old.
                            Lets you do data migration before completing the upgrade.
                          </Text>
                        </TeachingPopoverBody>
                        <TeachingPopoverFooter
                          primary={
                            <a href="https://learn.microsoft.com/en-us/power-apps/maker/data-platform/update-solutions"
                              target="_blank"
                              rel="noreferrer"
                              style={{ color: 'inherit', textDecoration: 'underline' }}
                            >Upgrade or update a solution ↗</a>
                          }
                        />
                      </TeachingPopoverSurface>
                    </TeachingPopover>
                  </span>
                }
                size="small"
              >
                <Dropdown value={displaySettings.importMode} selectedOptions={[displaySettings.importMode]}
                  onOptionSelect={(_e, d) => set('importMode', (d.optionValue ?? 'Update') as PluginSettings['importMode'])} size="small">
                  <Option value="Update">Update (fastest, keeps unused components)</Option>
                  <Option value="Upgrade">Upgrade (removes unused components)</Option>
                  <Option value="StageForUpgrade">Stage for Upgrade (deferred cleanup)</Option>
                </Dropdown>
              </Field>
              <Switch
                label={<LabelWithHelp
                  label="Check dependencies before transfer"
                  body="Runs a dependency check against each target before importing. Warnings appear in the progress panel if required components are missing."
                  learnHref="https://learn.microsoft.com/en-us/power-platform/alm/dependency-tracking-solution-components"
                  learnLabel="Dependency tracking"
                />}
                checked={displaySettings.checkDependencies} onChange={(_e, d) => set('checkDependencies', d.checked)} />
              <Switch
                label={<LabelWithHelp
                  label="Convert to managed"
                  body="Converts an unmanaged solution to managed during import. Only use this if you intentionally want to lock down customisation on the target."
                  learnHref="https://learn.microsoft.com/en-us/power-platform/alm/solution-concepts-alm#managed-and-unmanaged-solutions"
                  learnLabel="Managed vs unmanaged"
                />}
                checked={displaySettings.convertToManaged} onChange={(_e, d) => set('convertToManaged', d.checked)} />
              <Switch label="Deploy missing packages" checked={displaySettings.deployMissingPackages} onChange={(_e, d) => set('deployMissingPackages', d.checked)} />
              <Switch
                label={<LabelWithHelp
                  label="Overwrite unmanaged customisations"
                  body="⚠ Overwrites any unmanaged customisations on components in the target environment. Recommended unless the target has bespoke changes you need to keep."
                  learnHref="https://learn.microsoft.com/en-us/power-apps/maker/data-platform/update-solutions#overwrite-customizations-option"
                  learnLabel="Overwrite customisations"
                />}
                checked={displaySettings.overwriteUnmanaged} onChange={(_e, d) => set('overwriteUnmanaged', d.checked)} />
              <Switch
                label={<LabelWithHelp
                  label="Publish workflows / flows"
                  body="Automatically enables Power Automate flows and plug-ins included in the solution after import. Recommended for most deployments."
                  learnHref="https://learn.microsoft.com/en-us/power-apps/maker/data-platform/update-solutions"
                  learnLabel="Import options"
                />}
                checked={displaySettings.publishWorkflows} onChange={(_e, d) => set('publishWorkflows', d.checked)} />
              <Switch
                label={<LabelWithHelp
                  label="Skip product update dependencies"
                  body="Skips validation of dependencies introduced by Microsoft product updates. Only enable if you are experiencing transient dependency errors caused by Microsoft update timing."
                />}
                checked={displaySettings.skipProductUpdateDeps} onChange={(_e, d) => set('skipProductUpdateDeps', d.checked)} />
            </AccordionPanel>
          </AccordionItem>

          <AccordionItem value="publish">
            <AccordionHeader icon={<CheckmarkCircleRegular />} >Publish</AccordionHeader>
            <AccordionPanel className={styles.settingRow}>
              <Switch label="Publish customizations" checked={displaySettings.publishCustomizations} onChange={(_e, d) => set('publishCustomizations', d.checked)} />
            </AccordionPanel>
          </AccordionItem>

          <AccordionItem value="version">
            <AccordionHeader icon={<NumberSymbolRegular />} >Version</AccordionHeader>
            <AccordionPanel className={styles.settingRow}>
              <Field label="Update version" size="small">
                <Dropdown value={displaySettings.updateVersion} selectedOptions={[displaySettings.updateVersion]}
                  onOptionSelect={(_e, d) => set('updateVersion', (d.optionValue ?? 'Prompt') as PluginSettings['updateVersion'])} size="small">
                  <Option value="No">No</Option>
                  <Option value="Yes">Yes</Option>
                  <Option value="Prompt">Prompt</Option>
                </Dropdown>
              </Field>
              <Field label="Version policy" size="small">
                <Dropdown value={displaySettings.versionPolicy} selectedOptions={[displaySettings.versionPolicy]}
                  onOptionSelect={(_e, d) => set('versionPolicy', (d.optionValue ?? 'Date') as PluginSettings['versionPolicy'])} size="small">
                  <Option value="Major">Major (x.0.0.0)</Option>
                  <Option value="Minor">Minor (0.x.0.0)</Option>
                  <Option value="Build">Build (0.0.x.0)</Option>
                  <Option value="Revision">Revision (0.0.0.x)</Option>
                  <Option value="Manual">Manual</Option>
                  <Option value="Date">Date (yyyy.MM.dd.x)</Option>
                </Dropdown>
              </Field>
              {displaySettings.versionPolicy === 'Date' && (
                <Field label="Date mask" size="small">
                  <Input value={displaySettings.dateVersionMask} size="small" onChange={(_e, d) => set('dateVersionMask', d.value)} />
                </Field>
              )}
            </AccordionPanel>
          </AccordionItem>

        </Accordion>
    </Panel>
  );
}
