import {
  Text, ProgressBar,
  makeStyles, tokens, Button,
} from '@fluentui/react-components';
import {
  CheckmarkCircleFilled, DismissCircleFilled,
  ArrowClockwiseRegular,
} from '@fluentui/react-icons';
import { postMessage } from '../bridge';

const useStyles = makeStyles({
  root: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    backgroundColor: tokens.colorNeutralBackground2,
    borderLeft: `1px solid ${tokens.colorNeutralStroke1}`,
    overflow: 'auto',
    width: '300px',
    minWidth: '300px',
  },
  header: {
    padding: '8px 12px',
    fontWeight: 600,
    fontSize: '12px',
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground3,
  },
  emptyState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    flex: 1,
    padding: '20px',
    gap: '4px',
    color: tokens.colorNeutralForeground4,
  },
  itemList: {
    display: 'flex',
    flexDirection: 'column',
    padding: '4px 0',
  },
  item: {
    display: 'flex',
    flexDirection: 'column',
    gap: '4px',
    padding: '8px 12px',
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  itemHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: '6px',
  },
  itemAction: {
    fontWeight: 600,
    fontSize: '11px',
    flex: 1,
  },
  itemDirection: {
    fontSize: '10px',
    color: tokens.colorNeutralForeground3,
  },
  itemStatus: {
    fontSize: '10px',
    color: tokens.colorNeutralForeground3,
  },
  successIcon: { color: tokens.colorPaletteGreenForeground1 },
  errorIcon: { color: tokens.colorPaletteRedForeground1 },
  retryBar: {
    padding: '8px 12px',
    borderTop: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground3,
  },
});

export interface ProgressItemData {
  id: string;
  action: string;
  direction: string;
  status: 'pending' | 'running' | 'success' | 'error' | 'timeout' | 'skipped';
  percentage?: number;
  elapsed?: string;
  errorMessage?: string;
}

interface ProgressPanelProps {
  items: ProgressItemData[];
  visible: boolean;
  showRetry?: boolean;
}

export function ProgressPanel({ items, visible, showRetry }: ProgressPanelProps) {
  const styles = useStyles();

  if (!visible) return null;

  return (
    <div className={styles.root}>
      <div className={styles.header}>Transfer Progress</div>
      {items.length === 0 ? (
        <div className={styles.emptyState}>
          <Text size={200}>Start a transfer to see progress</Text>
        </div>
      ) : (
        <div className={styles.itemList}>
          {items.map((item) => (
            <div key={item.id} className={styles.item}>
              <div className={styles.itemHeader}>
                {item.status === 'success' && <CheckmarkCircleFilled className={styles.successIcon} fontSize={16} />}
                {item.status === 'error' && <DismissCircleFilled className={styles.errorIcon} fontSize={16} />}
                <Text className={styles.itemAction} truncate wrap={false} title={item.action}>{item.action}</Text>
              </div>
              <Text className={styles.itemDirection}>{item.direction}</Text>

              {item.status === 'running' && (
                <ProgressBar
                  value={item.percentage !== undefined ? item.percentage / 100 : undefined}
                  color="brand"
                  thickness="medium"
                />
              )}

              {item.status === 'running' && (
                <Text className={styles.itemStatus}>{item.elapsed}</Text>
              )}

              {item.status === 'error' && item.elapsed && (
                <Text className={styles.itemStatus} style={{ color: tokens.colorPaletteRedForeground1 }}>
                  {item.elapsed}
                </Text>
              )}

              {item.status === 'success' && (
                <ProgressBar value={1} color="success" thickness="medium" />
              )}

              {item.status === 'error' && (
                <ProgressBar value={1} color="error" thickness="medium" />
              )}
            </div>
          ))}
        </div>
      )}
      {showRetry && (
        <div className={styles.retryBar}>
          <Button size="small" appearance="primary" icon={<ArrowClockwiseRegular />}
            onClick={() => postMessage({ action: 'retryTransfer' } as never)}>
            Retry
          </Button>
        </div>
      )}
    </div>
  );
}
