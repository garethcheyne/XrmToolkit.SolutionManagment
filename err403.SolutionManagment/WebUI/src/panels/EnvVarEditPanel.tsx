import { useState, useEffect } from 'react';
import {
  InlineDrawer, DrawerHeader, DrawerHeaderTitle, DrawerBody, DrawerFooter,
  Button, Field, Textarea, Input, Badge, Text, Divider,
  makeStyles, tokens,
} from '@fluentui/react-components';
import {
  DismissRegular, SaveRegular, CopyRegular,
} from '@fluentui/react-icons';
import type { TargetConnection } from '../types';
import { postMessage } from '../bridge';

const useStyles = makeStyles({
  drawer: {
    width: '320px',
    minWidth: '320px',
    borderLeft: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  body: {
    display: 'flex',
    flexDirection: 'column',
    gap: '12px',
  },
  sourceSection: {
    padding: '8px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: '6px',
  },
  targetSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: '8px',
  },
  targetItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: '4px',
    padding: '8px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: '6px',
  },
  footer: {
    display: 'flex',
    justifyContent: 'flex-end',
    gap: '8px',
  },
});

interface EnvVarEditPanelProps {
  open: boolean;
  onClose: () => void;
  variable: {
    displayName: string;
    schemaName: string;
    type: string;
    currentValue: string;
  } | null;
  targets: TargetConnection[];
  targetEnvVarData: Record<string, Array<{ schemaname: string; value: string; exists: boolean }>>;
}

export function EnvVarEditPanel({ open, onClose, variable, targets, targetEnvVarData }: EnvVarEditPanelProps) {
  const styles = useStyles();
  const [targetValues, setTargetValues] = useState<Record<string, string>>({});

  useEffect(() => {
    if (!variable) return;
    const vals: Record<string, string> = {};
    targets.forEach((t) => {
      const tVars = targetEnvVarData[t.name];
      const match = tVars?.find((v) => v.schemaname === variable.schemaName);
      vals[t.name] = match?.value ?? '';
    });
    setTargetValues(vals);
  }, [variable, targets, targetEnvVarData]);

  if (!open || !variable) return null;

  const isJson = variable.type === 'JSON';

  const handleCopyToAll = () => {
    const updated: Record<string, string> = {};
    targets.forEach((t) => { updated[t.name] = variable.currentValue; });
    setTargetValues(updated);
  };

  const handleSave = () => {
    const changedValues: Record<string, string> = {};
    targets.forEach((t) => {
      const original = targetEnvVarData[t.name]?.find((v) => v.schemaname === variable.schemaName)?.value ?? '';
      if (targetValues[t.name] !== original) {
        changedValues[t.name] = targetValues[t.name] ?? '';
      }
    });

    if (Object.keys(changedValues).length > 0) {
      postMessage({
        action: 'saveEnvVar' as never,
        schemaName: variable.schemaName,
        displayName: variable.displayName,
        changedValues,
      } as never);
    }
    onClose();
  };

  return (
    <InlineDrawer open={open} position="end" className={styles.drawer}>
      <DrawerHeader>
        <DrawerHeaderTitle
          action={<Button appearance="subtle" icon={<DismissRegular />} onClick={onClose} size="small" />}
        >
          Edit Variable
        </DrawerHeaderTitle>
      </DrawerHeader>

      <DrawerBody className={styles.body}>
        <Text weight="semibold">{variable.displayName}</Text>
        <Badge size="small" appearance="tint" color="informative">{variable.type}</Badge>
        <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>{variable.schemaName}</Text>

        <Divider />

        <Field label="Source Value">
          <div className={styles.sourceSection}>
            {isJson ? (
              <Textarea value={variable.currentValue || '(default)'} readOnly rows={4} resize="vertical" />
            ) : (
              <Input value={variable.currentValue || '(default)'} readOnly />
            )}
          </div>
        </Field>

        <Button size="small" icon={<CopyRegular />} onClick={handleCopyToAll}>
          Copy source to all targets
        </Button>

        <Divider />

        <div className={styles.targetSection}>
          {targets.map((t) => (
            <div key={t.name} className={styles.targetItem}>
              <Text size={200} weight="semibold">{t.name}</Text>
              {isJson ? (
                <Textarea
                  value={targetValues[t.name] ?? ''}
                  onChange={(_e, d) => setTargetValues({ ...targetValues, [t.name]: d.value })}
                  rows={3}
                  resize="vertical"
                />
              ) : (
                <Input
                  value={targetValues[t.name] ?? ''}
                  onChange={(_e, d) => setTargetValues({ ...targetValues, [t.name]: d.value })}
                />
              )}
            </div>
          ))}
        </div>
      </DrawerBody>

      <DrawerFooter className={styles.footer}>
        <Button appearance="secondary" onClick={onClose}>Cancel</Button>
        <Button appearance="primary" icon={<SaveRegular />} onClick={handleSave}>Save Changes</Button>
      </DrawerFooter>
    </InlineDrawer>
  );
}
