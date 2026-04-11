import {
  Dialog, DialogSurface, DialogTitle, DialogBody,
  DialogContent, DialogActions,
  Button, Text, Badge, Divider,
  makeStyles, tokens,
} from '@fluentui/react-components';
import {
  ArrowUploadRegular,
  LockClosedFilled, LockOpenFilled,
  CheckmarkCircleFilled, DismissCircleFilled,
} from '@fluentui/react-icons';
import type { SelectedSolution, TargetConnection } from '../types';
import type { PluginSettings } from './SettingsDrawer';
import { postMessage } from '../bridge';

const useStyles = makeStyles({
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: '12px',
    maxWidth: '520px',
  },
  sectionTitle: {
    fontSize: '11px',
    fontWeight: 600,
    color: tokens.colorNeutralForeground3,
    textTransform: 'uppercase' as const,
    letterSpacing: '0.5px',
  },
  solutionList: {
    display: 'flex',
    flexDirection: 'column',
    gap: '3px',
    padding: '8px 10px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: '6px',
    maxHeight: '150px',
    overflow: 'auto',
  },
  solutionItem: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  targetChips: {
    display: 'flex',
    gap: '4px',
    flexWrap: 'wrap',
  },
  settingsGrid: {
    display: 'grid',
    gridTemplateColumns: '1fr auto',
    gap: '2px 12px',
    padding: '8px 10px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: '6px',
    fontSize: '12px',
    alignItems: 'center',
  },
  settingLabel: {
    color: tokens.colorNeutralForeground3,
  },
  settingValue: {
    display: 'flex',
    alignItems: 'center',
    gap: '4px',
    fontWeight: 600,
    justifyContent: 'flex-end',
  },
});

const useBoolStyles = makeStyles({
  root: { display: 'flex', alignItems: 'center', gap: '3px', fontWeight: 600, fontSize: '12px', justifyContent: 'flex-end' },
  yes: { color: tokens.colorPaletteGreenForeground1 },
  no: { color: tokens.colorPaletteRedForeground1 },
});

function BoolValue({ value, label }: { value: boolean; label?: string }) {
  const bs = useBoolStyles();
  return (
    <span className={`${bs.root} ${value ? bs.yes : bs.no}`}>
      {value ? <CheckmarkCircleFilled fontSize={14} /> : <DismissCircleFilled fontSize={14} />}
      {label ?? (value ? 'Yes' : 'No')}
    </span>
  );
}

interface TransferConfirmDialogProps {
  solutions: SelectedSolution[];
  targets: TargetConnection[];
  settings: PluginSettings;
  open: boolean;
  onClose: () => void;
}

export function TransferConfirmDialog({ solutions, targets, settings, open, onClose }: TransferConfirmDialogProps) {
  const styles = useStyles();
  const totalOps = solutions.length * targets.length;

  const handleConfirm = () => {
    postMessage({
      action: 'startTransfer',
      solutions,
      settings: {
        managed: settings.managed,
        importMode: settings.importMode,
        overwriteUnmanaged: settings.overwriteUnmanaged,
        publishWorkflows: settings.publishWorkflows,
        checkDependencies: settings.checkDependencies,
        convertToManaged: settings.convertToManaged,
      },
    });
    onClose();
  };

  return (
    <Dialog open={open} onOpenChange={(_e, data) => { if (!data.open) onClose(); }}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>Confirm Transfer</DialogTitle>
          <DialogContent className={styles.content}>

            <Text className={styles.sectionTitle}>Solutions ({solutions.length})</Text>
            <div className={styles.solutionList}>
              {solutions.map((s) => (
                <div key={s.solutionId} className={styles.solutionItem}>
                  <Text size={200} weight="semibold">{s.friendlyName}</Text>
                  <Badge size="small" appearance="tint" color="informative">{s.version}</Badge>
                </div>
              ))}
            </div>

            <Text className={styles.sectionTitle}>Targets ({targets.length})</Text>
            <div className={styles.targetChips}>
              {targets.map((t) => (
                <Badge key={t.name} size="medium" appearance="filled" color="brand">{t.name}</Badge>
              ))}
            </div>

            <Divider />

            <Text className={styles.sectionTitle}>Transfer Settings</Text>
            <div className={styles.settingsGrid}>
              <span className={styles.settingLabel}>Export as</span>
              <span className={styles.settingValue}>
                {settings.managed
                  ? <><LockClosedFilled fontSize={14} color={tokens.colorBrandForeground1} /> Managed</>
                  : <><LockOpenFilled fontSize={14} color={tokens.colorPaletteYellowForeground2} /> Unmanaged</>}
              </span>

              <span className={styles.settingLabel}>Import mode</span>
              <span className={styles.settingValue}>{settings.importMode}</span>

              <span className={styles.settingLabel}>Overwrite unmanaged</span>
              <BoolValue value={settings.overwriteUnmanaged} />

              <span className={styles.settingLabel}>Publish workflows</span>
              <BoolValue value={settings.publishWorkflows} />

              <span className={styles.settingLabel}>Check dependencies</span>
              <BoolValue value={settings.checkDependencies} />

              <span className={styles.settingLabel}>Convert to managed</span>
              <BoolValue value={settings.convertToManaged} />

              <span className={styles.settingLabel}>Deploy missing packages</span>
              <BoolValue value={settings.deployMissingPackages} />

              <span className={styles.settingLabel}>Skip product update deps</span>
              <BoolValue value={settings.skipProductUpdateDeps} />

              <span className={styles.settingLabel}>Publish customizations</span>
              <BoolValue value={settings.publishCustomizations} />

              <span className={styles.settingLabel}>Update version</span>
              <span className={styles.settingValue}>{settings.updateVersion}</span>

              {settings.updateVersion !== 'No' && (
                <>
                  <span className={styles.settingLabel}>Version policy</span>
                  <span className={styles.settingValue}>{settings.versionPolicy}</span>
                </>
              )}

              <span className={styles.settingLabel}>Total operations</span>
              <span className={styles.settingValue}>{totalOps}</span>
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
