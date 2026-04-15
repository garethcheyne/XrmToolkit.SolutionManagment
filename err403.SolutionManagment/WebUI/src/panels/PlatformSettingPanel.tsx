import { Text, Badge, Divider, makeStyles, tokens } from '@fluentui/react-components';
import { Panel } from '../components/Panel';
import type { TargetConnection } from '../types';
import type { SettingRow } from '../tabs/PlatformSettingsTab';

const useStyles = makeStyles({
  field: {
    display: 'flex',
    flexDirection: 'column',
    gap: '2px',
    padding: '4px 0',
  },
  label: {
    fontSize: '11px',
    fontWeight: 600,
    color: tokens.colorNeutralForeground3,
  },
  value: {
    padding: '4px 8px',
    backgroundColor: tokens.colorNeutralBackground4,
    borderRadius: '4px',
    fontSize: '13px',
    wordBreak: 'break-all',
  },
  description: {
    fontSize: '12px',
    color: tokens.colorNeutralForeground3,
    fontStyle: 'italic',
    padding: '4px 0',
  },
  matchValue: { color: tokens.colorPaletteGreenForeground1 },
  mismatchValue: { color: tokens.colorPaletteRedForeground1, fontWeight: 600 },
  notFound: { color: tokens.colorNeutralForeground4, fontStyle: 'italic' },
});

interface PlatformSettingPanelProps {
  setting: SettingRow;
  targets: TargetConnection[];
  targetOrgSettingsData: Record<string, Record<string, unknown>>;
  onClose: () => void;
}

export function PlatformSettingPanel({ setting, targets, targetOrgSettingsData, onClose }: PlatformSettingPanelProps) {
  const styles = useStyles();

  return (
    <Panel title={setting.displayName} onClose={onClose}>
      <div className={styles.field}>
        <Text className={styles.label}>Schema Name</Text>
        <Text size={200}>{setting.key}</Text>
      </div>

      <div className={styles.field}>
        <Text className={styles.label}>Category</Text>
        <Badge appearance="tint" color="informative" size="small">{setting.category}</Badge>
      </div>

      <div className={styles.field}>
        <Text className={styles.label}>Type</Text>
        <Text size={200}>{setting.type}</Text>
      </div>

      {setting.description && (
        <div className={styles.field}>
          <Text className={styles.label}>Description</Text>
          <Text className={styles.description}>{setting.description}</Text>
        </div>
      )}

      <Divider />

      <div className={styles.field}>
        <Text className={styles.label}>Source Value</Text>
        <div className={styles.value}>{setting.value}</div>
      </div>

      {setting.defaultValue && (
        <div className={styles.field}>
          <Text className={styles.label}>Default Value</Text>
          <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>{setting.defaultValue}</Text>
        </div>
      )}

      {targets.length > 0 && (
        <>
          <Divider />
          <Text className={styles.label} style={{ paddingTop: '4px' }}>Target Values</Text>
          {targets.map((t) => {
            const tData = targetOrgSettingsData[t.name];
            const tVal = tData?.[setting.key];
            const tStr = tVal !== undefined && tVal !== null ? String(tVal) : null;
            const isMatch = tStr === setting.value;

            return (
              <div key={t.name} className={styles.field}>
                <Text size={200} weight="semibold">{t.name}</Text>
                {tStr === null ? (
                  <Text size={200} className={styles.notFound}>not available</Text>
                ) : (
                  <div className={`${styles.value} ${isMatch ? styles.matchValue : styles.mismatchValue}`}>
                    {tStr}
                  </div>
                )}
              </div>
            );
          })}
        </>
      )}
    </Panel>
  );
}
