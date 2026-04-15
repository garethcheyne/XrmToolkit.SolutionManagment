import { useState, useEffect, useMemo } from 'react';
import {
  Button, Field, Textarea, Input, Badge, Text, Divider,
  makeStyles, tokens,
} from '@fluentui/react-components';
import {
  SaveRegular, CopyRegular,
} from '@fluentui/react-icons';
import type { TargetConnection } from '../types';
import { postMessage } from '../bridge';
import { Panel } from '../components/Panel';

const useStyles = makeStyles({
  sourceSection: {
    padding: '8px',
    backgroundColor: tokens.colorNeutralBackground4,
    borderRadius: '6px',
    '& input, & textarea': { width: '100%' },
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
    backgroundColor: tokens.colorNeutralBackground4,
    borderRadius: '6px',
    '& input, & textarea': { width: '100%' },
  },
  targetItemDirty: {
    display: 'flex',
    flexDirection: 'column',
    gap: '4px',
    padding: '8px',
    backgroundColor: tokens.colorPaletteYellowBackground1,
    borderRadius: '6px',
    borderLeft: `3px solid ${tokens.colorPaletteYellowBorder1}`,
    '& input, & textarea': { width: '100%' },
  },
  targetHeader: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  divider:{
    flexGrow: 0,
  }
});

interface EnvVarEditPanelProps {
  open: boolean;
  onClose: () => void;
  variable: {
    displayName: string;
    schemaName: string;
    description: string;
    type: string;
    defaultValue: string;
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

  // Compute original values and dirty state per target
  const originals = useMemo(() => {
    const map: Record<string, string> = {};
    targets.forEach((t) => {
      map[t.name] = targetEnvVarData[t.name]?.find((v) => v.schemaname === variable.schemaName)?.value ?? '';
    });
    return map;
  }, [variable, targets, targetEnvVarData]);

  const dirtyTargets = useMemo(() => {
    const set = new Set<string>();
    targets.forEach((t) => {
      if ((targetValues[t.name] ?? '') !== (originals[t.name] ?? '')) {
        set.add(t.name);
      }
    });
    return set;
  }, [targets, targetValues, originals]);

  const hasDirty = dirtyTargets.size > 0;

  const handleCopyToAll = () => {
    const effectiveValue = variable.currentValue || variable.defaultValue;
    const updated: Record<string, string> = {};
    targets.forEach((t) => { updated[t.name] = effectiveValue; });
    setTargetValues(updated);
  };

  const handleSave = () => {
    if (!hasDirty) return;
    const changedValues: Record<string, string> = {};
    dirtyTargets.forEach((name) => {
      changedValues[name] = targetValues[name] ?? '';
    });

    postMessage({
      action: 'saveEnvVar',
      schemaName: variable.schemaName,
      displayName: variable.displayName,
      changedValues,
    });
    onClose();
  };

  const footer = (
    <>
      <Button appearance="secondary" onClick={onClose}>Cancel</Button>
      <Button appearance="primary" icon={<SaveRegular />} onClick={handleSave} disabled={!hasDirty}>
        Save Changes{hasDirty ? ` (${dirtyTargets.size})` : ''}
      </Button>
    </>
  );

  return (
    <Panel title="Edit Variable" onClose={onClose} footer={footer}>
        <Text weight="semibold">{variable.displayName}</Text>
        <Badge size="small" appearance="tint" color="informative" style={{ alignSelf: 'flex-start' }}>{variable.type}</Badge>
        <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>{variable.schemaName}</Text>

        {variable.description && (
          <Text size={200} style={{ color: tokens.colorNeutralForeground2, fontStyle: 'italic' }}>{variable.description}</Text>
        )}

        <Divider className={styles.divider} />

        <Field label="Default Value">
          {isJson ? (
            <Textarea value={variable.defaultValue || '(none)'} readOnly rows={3} resize="vertical" />
          ) : (
            <Input value={variable.defaultValue || '(none)'} readOnly />
          )}
        </Field>
        <Field label="Current Value">
          {isJson ? (
            <Textarea value={variable.currentValue || '(default)'} readOnly rows={3} resize="vertical" />
          ) : (
            <Input value={variable.currentValue || '(default)'} readOnly />
          )}
        </Field>

        <Button size="small" icon={<CopyRegular />} onClick={handleCopyToAll} style={{ alignSelf: 'flex-start' }}>
          Copy source to all targets
        </Button>

        <Divider className={styles.divider} />

        <div className={styles.targetSection}>
          {targets.map((t) => {
            const isDirty = dirtyTargets.has(t.name);
            return (
              <div key={t.name} className={isDirty ? styles.targetItemDirty : styles.targetItem}>
                <div className={styles.targetHeader}>
                  <Text size={200} weight="semibold">{t.name}</Text>
                  {isDirty && <Badge size="small" appearance="filled" color="warning">modified</Badge>}
                </div>
                <Field label="Current" size="small">
                  <Input value={originals[t.name] || '(not set)'} readOnly size="small" />
                </Field>
                <Field label="New Value" size="small">
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
                </Field>
              </div>
            );
          })}
        </div>
    </Panel>
  );
}
