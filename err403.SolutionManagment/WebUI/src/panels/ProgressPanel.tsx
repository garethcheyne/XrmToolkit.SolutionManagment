import {
  Text,
  Spinner,
  makeStyles,
  tokens,
  Button,
  mergeClasses,
} from '@fluentui/react-components';
import {
  CheckmarkCircleFilled,
  DismissCircleFilled,
  WarningFilled,
  ArrowDownloadRegular,
  DocumentSearchRegular,
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
  },
  header: {
    padding: '10px 14px',
    fontWeight: 600,
    fontSize: '13px',
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
    alignItems: 'flex-start',
    gap: '10px',
    padding: '8px 14px',
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  itemIcon: {
    flexShrink: 0,
    marginTop: '2px',
  },
  itemContent: {
    flex: 1,
    minWidth: 0,
  },
  itemAction: {
    fontWeight: 600,
    fontSize: '12px',
  },
  itemDirection: {
    fontSize: '11px',
    color: tokens.colorNeutralForeground3,
  },
  itemStatus: {
    fontSize: '11px',
    color: tokens.colorNeutralForeground3,
    marginTop: '2px',
  },
  itemPercentage: {
    fontSize: '11px',
    fontWeight: 600,
    color: tokens.colorBrandForeground1,
  },
  itemLinks: {
    display: 'flex',
    gap: '8px',
    marginTop: '4px',
  },
  successIcon: {
    color: tokens.colorPaletteGreenForeground1,
  },
  errorIcon: {
    color: tokens.colorPaletteRedForeground1,
  },
  warningIcon: {
    color: tokens.colorPaletteYellowForeground1,
  },
  retryBar: {
    padding: '8px 14px',
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
  showDownloadLog?: boolean;
  showViewMessage?: boolean;
  showDownloadSolution?: boolean;
  importJobId?: string;
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
      <div className={styles.header}>Progress</div>
      {items.length === 0 ? (
        <div className={styles.emptyState}>
          <Text size={200}>Start a solution transfer to see progress</Text>
        </div>
      ) : (
        <div className={styles.itemList}>
          {items.map((item) => (
            <div key={item.id} className={styles.item}>
              <div className={styles.itemIcon}>
                {item.status === 'running' && <Spinner size="tiny" />}
                {item.status === 'success' && (
                  <CheckmarkCircleFilled className={styles.successIcon} fontSize={18} />
                )}
                {item.status === 'error' && (
                  <DismissCircleFilled className={styles.errorIcon} fontSize={18} />
                )}
                {item.status === 'timeout' && (
                  <WarningFilled className={styles.warningIcon} fontSize={18} />
                )}
                {item.status === 'skipped' && (
                  <WarningFilled className={styles.warningIcon} fontSize={18} />
                )}
                {item.status === 'pending' && (
                  <span style={{ width: 18, height: 18, display: 'inline-block' }} />
                )}
              </div>
              <div className={styles.itemContent}>
                <Text className={styles.itemAction} block truncate wrap={false}>
                  {item.action}
                </Text>
                <Text className={styles.itemDirection} block>
                  {item.direction}
                </Text>
                <Text className={styles.itemStatus} block>
                  {item.percentage !== undefined && item.status === 'running' && (
                    <span className={styles.itemPercentage}>{item.percentage}% </span>
                  )}
                  {item.elapsed ?? ''}
                </Text>
                {(item.showDownloadLog || item.showViewMessage || item.showDownloadSolution) && (
                  <div className={styles.itemLinks}>
                    {item.showDownloadLog && (
                      <Button
                        size="small"
                        appearance="subtle"
                        icon={<ArrowDownloadRegular />}
                        onClick={() =>
                          postMessage({ action: 'downloadLog', id: item.id } as never)
                        }
                      >
                        Download log
                      </Button>
                    )}
                    {item.showViewMessage && (
                      <Button
                        size="small"
                        appearance="subtle"
                        icon={<DocumentSearchRegular />}
                        onClick={() =>
                          postMessage({ action: 'viewMessage', id: item.id } as never)
                        }
                      >
                        View message
                      </Button>
                    )}
                    {item.showDownloadSolution && (
                      <Button
                        size="small"
                        appearance="subtle"
                        icon={<ArrowDownloadRegular />}
                        onClick={() =>
                          postMessage({ action: 'downloadSolution', id: item.id } as never)
                        }
                      >
                        Download solution
                      </Button>
                    )}
                  </div>
                )}
                {item.status === 'error' && item.errorMessage && (
                  <Text
                    size={200}
                    className={mergeClasses(styles.itemStatus)}
                    style={{ color: tokens.colorPaletteRedForeground1 }}
                    block
                  >
                    {item.errorMessage}
                  </Text>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
      {showRetry && (
        <div className={styles.retryBar}>
          <Button
            size="small"
            appearance="primary"
            icon={<ArrowClockwiseRegular />}
            onClick={() => postMessage({ action: 'retryTransfer' } as never)}
          >
            Retry
          </Button>
        </div>
      )}
    </div>
  );
}
