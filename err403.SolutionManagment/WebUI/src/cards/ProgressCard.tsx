import { useState } from 'react';
import {
  Text, ProgressBar, Button,
  makeStyles, tokens,
} from '@fluentui/react-components';
import {
  CheckmarkCircleFilled, DismissCircleFilled,
  ArrowDownloadRegular, EyeRegular,
} from '@fluentui/react-icons';
import { ErrorDetailDialog } from '../dialogs/ErrorDetailDialog';
import type { ProgressItemData } from '../panels/ProgressPanel';

const useStyles = makeStyles({
  item: {
    display: 'flex',
    flexDirection: 'column',
    gap: '3px',
    padding: '4px 8px',
    borderRadius: '4px',
    backgroundColor: tokens.colorNeutralBackground4,
    marginBottom: '2px',
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
  itemStatus: {
    fontSize: '10px',
    color: tokens.colorNeutralForeground3,
  },
  successIcon: { color: tokens.colorPaletteGreenForeground1 },
  errorIcon: { color: tokens.colorPaletteRedForeground1 },
  errorActions: {
    display: 'flex',
    gap: '4px',
    alignItems: 'center',
    paddingTop: '2px',
  },
  errorLabel: {
    fontSize: '11px',
    color: tokens.colorPaletteRedForeground1,
    flex: 1,
  },
});

export function formatElapsed(ms: number): string {
  const totalSeconds = Math.floor(ms / 1000);
  if (totalSeconds < 60) return `${totalSeconds}s`;
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  if (minutes < 60) return `${minutes}m ${seconds.toString().padStart(2, '0')}s`;
  const hours = Math.floor(minutes / 60);
  const remainMins = minutes % 60;
  return `${hours}h ${remainMins.toString().padStart(2, '0')}m`;
}

function downloadError(title: string, errorMessage: string) {
  const blob = new Blob([errorMessage], { type: 'text/plain;charset=utf-8' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `${title.replace(/[^a-zA-Z0-9_-]/g, '_')}_error.txt`;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
}

interface ProgressCardProps {
  item: ProgressItemData;
  now: number;
}

export function ProgressCard({ item, now }: ProgressCardProps) {
  const styles = useStyles();
  const [errorDialogOpen, setErrorDialogOpen] = useState(false);

  // Compute elapsed: for running items, calculate live from startedAt
  let elapsedLabel: string | undefined;
  if (item.startedAt && item.status === 'running') {
    const startMs = new Date(item.startedAt).getTime();
    elapsedLabel = formatElapsed(now - startMs);
  } else if (item.elapsedMs != null && (item.status === 'success' || item.status === 'error')) {
    elapsedLabel = formatElapsed(item.elapsedMs);
  }

  const hasErrorDetail = item.status === 'error' && !!item.errorMessage;

  return (
    <>
      <div className={styles.item}>
        <div className={styles.itemHeader}>
          {item.status === 'success' && <CheckmarkCircleFilled className={styles.successIcon} fontSize={14} />}
          {item.status === 'error' && <DismissCircleFilled className={styles.errorIcon} fontSize={14} />}
          <Text className={styles.itemAction} truncate wrap={false} title={item.action}>{item.action}</Text>
          {elapsedLabel && (
            <Text className={styles.itemStatus}>{elapsedLabel}</Text>
          )}
        </div>
        {item.status === 'running' && (
          <ProgressBar
            value={item.percentage ? item.percentage / 100 : undefined}
            color="brand"
            thickness="large"
          />
        )}
        {item.status === 'running' && item.elapsed && (
          <Text className={styles.itemStatus}>{item.elapsed}</Text>
        )}
        {item.status === 'success' && (
          <ProgressBar value={1} color="success" thickness="large" />
        )}
        {item.status === 'error' && (
          <>
            <ProgressBar value={1} color="error" thickness="large" />
            {hasErrorDetail ? (
              <div className={styles.errorActions}>
                <Text className={styles.errorLabel}>Failed</Text>
                <Button
                  size="small"
                  appearance="subtle"
                  icon={<EyeRegular />}
                  onClick={() => setErrorDialogOpen(true)}
                >
                  View
                </Button>
                <Button
                  size="small"
                  appearance="subtle"
                  icon={<ArrowDownloadRegular />}
                  onClick={() => downloadError(
                    `${item.action}_${item.target ?? ''}`,
                    item.errorMessage!,
                  )}
                >
                  Download
                </Button>
              </div>
            ) : item.elapsed ? (
              <Text className={styles.errorLabel}>{item.elapsed}</Text>
            ) : null}
          </>
        )}
      </div>

      {hasErrorDetail && (
        <ErrorDetailDialog
          open={errorDialogOpen}
          title={`Import Error — ${item.action}${item.target ? ` → ${item.target}` : ''}`}
          errorMessage={item.errorMessage!}
          onClose={() => setErrorDialogOpen(false)}
        />
      )}
    </>
  );
}
