import {
  Button,
  Tag,
  TagGroup,
  Text,
  makeStyles,
  tokens,
  Tooltip,
  type TagGroupProps,
} from '@fluentui/react-components';
import {
  AddRegular,
  DismissRegular,
  CircleFilled,
  PlugConnectedRegular,
} from '@fluentui/react-icons';
import { postMessage } from '../bridge';
import type { SourceConnection, TargetConnection } from '../types';

const useStyles = makeStyles({
  root: {
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
    padding: '6px 12px',
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground3,
    flexShrink: 0,
    minHeight: '40px',
    flexWrap: 'wrap',
  },
  sourceSection: {
    display: 'flex',
    alignItems: 'center',
    gap: '6px',
    marginRight: '8px',
  },
  sourceLabel: {
    fontSize: '11px',
    fontWeight: 600,
    textTransform: 'uppercase' as const,
    letterSpacing: '0.5px',
    color: tokens.colorNeutralForeground3,
  },
  sourceName: {
    fontWeight: 600,
    fontSize: '13px',
  },
  connectedDot: {
    color: tokens.colorPaletteGreenForeground1,
    fontSize: '8px',
  },
  disconnectedDot: {
    color: tokens.colorPaletteRedForeground1,
    fontSize: '8px',
  },
  divider: {
    width: '1px',
    height: '24px',
    backgroundColor: tokens.colorNeutralStroke2,
    marginLeft: '4px',
    marginRight: '4px',
  },
  targetSection: {
    display: 'flex',
    alignItems: 'center',
    gap: '6px',
    flexWrap: 'wrap',
    flex: 1,
  },
  targetLabel: {
    fontSize: '11px',
    fontWeight: 600,
    textTransform: 'uppercase' as const,
    letterSpacing: '0.5px',
    color: tokens.colorNeutralForeground3,
  },
});

interface ConnectionBarProps {
  source: SourceConnection;
  targets: TargetConnection[];
  onAbout?: () => void;
}

export function ConnectionBar({ source, targets, onAbout }: ConnectionBarProps) {
  const styles = useStyles();

  return (
    <div className={styles.root}>
      {/* Source */}
      <div className={styles.sourceSection}>
        <Text className={styles.sourceLabel}>Source</Text>
        <CircleFilled
          className={source.isConnected ? styles.connectedDot : styles.disconnectedDot}
        />
        <Text className={styles.sourceName}>
          {source.isConnected ? source.name : 'Not connected'}
        </Text>
      </div>

      <div className={styles.divider} />

      {/* Targets */}
      <div className={styles.targetSection}>
        <Text className={styles.targetLabel}>Target(s)</Text>
        <TagGroup
          onDismiss={(_e: unknown, data: Parameters<NonNullable<TagGroupProps['onDismiss']>>[1]) => {
            postMessage({ action: 'removeTarget', connectionName: data.value });
          }}
          size="small"
        >
          {targets.map((t) => (
            <Tag
              key={t.name}
              value={t.name}
              shape="circular"
              appearance="brand"
              icon={<PlugConnectedRegular />}
              dismissible
              dismissIcon={<DismissRegular />}
            >
              {t.name}
            </Tag>
          ))}
        </TagGroup>
        {targets.length === 0 && (
          <Text size={200} style={{ color: tokens.colorNeutralForeground4, fontStyle: 'italic' }}>
            No targets — click Add
          </Text>
        )}
        <Tooltip content="Add target environment" relationship="label">
          <Button
            size="small"
            appearance="subtle"
            icon={<AddRegular />}
            onClick={() => postMessage({ action: 'addTarget' })}
          >
            Add
          </Button>
        </Tooltip>
      </div>
      <Button size="small" appearance="subtle" onClick={() => postMessage({ action: 'authenticateGds' })}
        style={{ marginLeft: '4px' }}>
        Authenticate
      </Button>
      {onAbout && (
        <Button size="small" appearance="subtle" onClick={onAbout}>
          About
        </Button>
      )}
    </div>
  );
}
