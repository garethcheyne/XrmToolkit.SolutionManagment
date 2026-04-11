import { useState } from 'react';
import {
  Dialog, DialogTrigger, DialogSurface, DialogTitle, DialogBody,
  DialogContent, DialogActions,
  Button, Switch, Dropdown, Option, Field, Text,
  Divider, makeStyles, tokens,
} from '@fluentui/react-components';
import {
  ArrowUploadRegular,
} from '@fluentui/react-icons';
import type { SelectedSolution, TransferSettings, TargetConnection } from '../types';
import { postMessage } from '../bridge';

const useStyles = makeStyles({
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: '16px',
  },
  solutionList: {
    display: 'flex',
    flexDirection: 'column',
    gap: '4px',
    padding: '8px 12px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: '6px',
    maxHeight: '200px',
    overflow: 'auto',
  },
  targetList: {
    display: 'flex',
    gap: '8px',
    flexWrap: 'wrap',
  },
  settingsGrid: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: '12px',
  },
});

interface TransferDialogProps {
  solutions: SelectedSolution[];
  targets: TargetConnection[];
  open: boolean;
  onClose: () => void;
}

export function TransferDialog({ solutions, targets, open, onClose }: TransferDialogProps) {
  const styles = useStyles();

  const [settings, setSettings] = useState<TransferSettings>({
    managed: true,
    importMode: 'Update',
    overwriteUnmanaged: true,
    publishWorkflows: true,
    checkDependencies: false,
    convertToManaged: false,
  });

  const handleTransfer = () => {
    postMessage({
      action: 'startTransfer',
      solutions,
      settings,
    });
    onClose();
  };

  return (
    <Dialog open={open} onOpenChange={(_e, data) => { if (!data.open) onClose(); }}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>Transfer Solutions</DialogTitle>
          <DialogContent className={styles.content}>

            <Field label="Solutions to transfer">
              <div className={styles.solutionList}>
                {solutions.map((s) => (
                  <Text key={s.solutionId} size={200}>
                    {s.friendlyName} ({s.version})
                  </Text>
                ))}
              </div>
            </Field>

            <Field label="Target environments">
              <div className={styles.targetList}>
                {targets.map((t) => (
                  <Text key={t.name} size={200} weight="semibold">
                    {t.name}
                  </Text>
                ))}
              </div>
            </Field>

            <Divider />

            <div className={styles.settingsGrid}>
              <Switch
                label="Export as managed"
                checked={settings.managed}
                onChange={(_e, d) => setSettings({ ...settings, managed: d.checked })}
              />
              <Switch
                label="Overwrite unmanaged"
                checked={settings.overwriteUnmanaged}
                onChange={(_e, d) => setSettings({ ...settings, overwriteUnmanaged: d.checked })}
              />
              <Switch
                label="Publish workflows"
                checked={settings.publishWorkflows}
                onChange={(_e, d) => setSettings({ ...settings, publishWorkflows: d.checked })}
              />
              <Switch
                label="Convert to managed"
                checked={settings.convertToManaged}
                onChange={(_e, d) => setSettings({ ...settings, convertToManaged: d.checked })}
              />
            </div>

            <Field label="Import mode">
              <Dropdown
                value={settings.importMode}
                selectedOptions={[settings.importMode]}
                onOptionSelect={(_e, d) =>
                  setSettings({ ...settings, importMode: (d.optionValue as TransferSettings['importMode']) ?? 'Update' })
                }
              >
                <Option value="Update">Update</Option>
                <Option value="StageForUpgrade">Stage for Upgrade</Option>
                <Option value="Upgrade">Upgrade</Option>
              </Dropdown>
            </Field>

          </DialogContent>
          <DialogActions>
            <DialogTrigger disableButtonEnhancement>
              <Button appearance="secondary">Cancel</Button>
            </DialogTrigger>
            <Button appearance="primary" icon={<ArrowUploadRegular />} onClick={handleTransfer}>
              Transfer {solutions.length} solution{solutions.length !== 1 ? 's' : ''} to {targets.length} target{targets.length !== 1 ? 's' : ''}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
