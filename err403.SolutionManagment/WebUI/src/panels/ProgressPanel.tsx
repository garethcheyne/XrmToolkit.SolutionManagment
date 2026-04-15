import { useMemo, useState, useEffect } from 'react';
import {
  Text,
  makeStyles, tokens, Button, Divider,
} from '@fluentui/react-components';
import {
  ArrowClockwiseRegular, ArrowDownloadRegular, ArrowUploadRegular,
  CloudArrowUpRegular,
} from '@fluentui/react-icons';
import { postMessage } from '../bridge';
import { Panel } from '../components/Panel';
import { ProgressCard } from '../cards/ProgressCard';

const useStyles = makeStyles({
  phaseSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: '2px',
  },
  phaseHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: '6px',
    padding: '6px 0',
    fontSize: '11px',
    fontWeight: 600,
    textTransform: 'uppercase',
    color: tokens.colorNeutralForeground3,
    letterSpacing: '0.5px',
  },
  phaseIcon: {
    fontSize: '14px',
    color: tokens.colorBrandForeground1,
  },
  targetGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: '2px',
    marginLeft: '8px',
    paddingLeft: '8px',
    borderLeft: `2px solid ${tokens.colorNeutralStroke2}`,
  },
  targetLabel: {
    fontSize: '10px',
    fontWeight: 600,
    color: tokens.colorNeutralForeground2,
    padding: '4px 0 2px 0',
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
  divider: { flexGrow: 0 },
});

export interface ProgressItemData {
  id: string;
  action: string;
  direction: string;
  target?: string;
  phase?: 'export' | 'import' | 'publish';
  status: 'pending' | 'running' | 'success' | 'error' | 'timeout' | 'skipped';
  percentage?: number;
  elapsed?: string;
  startedAt?: string;
  elapsedMs?: number;
  errorMessage?: string;
}

interface ProgressPanelProps {
  items: ProgressItemData[];
  visible: boolean;
  showRetry?: boolean;
  onClose: () => void;
}

export function ProgressPanel({ items, visible, showRetry, onClose }: ProgressPanelProps) {
  const styles = useStyles();

  // Tick every second to update live elapsed counters
  const [now, setNow] = useState(Date.now());
  const hasRunning = items.some((i) => i.status === 'running');
  useEffect(() => {
    if (!hasRunning) return;
    const id = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(id);
  }, [hasRunning]);

  const { exportItems, targetGroups } = useMemo(() => {
    const exports = items.filter((i) => (i.phase ?? 'export') === 'export');
    // Group import/publish by target
    const importPublish = items.filter((i) => i.phase === 'import' || i.phase === 'publish');
    const grouped = new Map<string, { imports: ProgressItemData[]; publishes: ProgressItemData[] }>();
    importPublish.forEach((i) => {
      const t = i.target ?? i.direction ?? '';
      if (!grouped.has(t)) grouped.set(t, { imports: [], publishes: [] });
      const g = grouped.get(t)!;
      if (i.phase === 'import') g.imports.push(i);
      else g.publishes.push(i);
    });
    return { exportItems: exports, targetGroups: grouped };
  }, [items]);

  if (!visible) return null;

  const hasTargets = targetGroups.size > 0;

  const footer = showRetry ? (
    <Button size="small" appearance="primary" icon={<ArrowClockwiseRegular />}
      onClick={() => postMessage({ action: 'retryTransfer' } as never)}>
      Retry
    </Button>
  ) : undefined;

  return (
    <Panel title="Transfer Progress" onClose={onClose} footer={footer}>
      {items.length === 0 ? (
        <div className={styles.emptyState}>
          <Text size={200}>Start a transfer to see progress</Text>
        </div>
      ) : (
        <>
          {/* Phase 1: Export (Source) */}
          <div className={styles.phaseSection}>
            <div className={styles.phaseHeader}>
              <ArrowUploadRegular className={styles.phaseIcon} />
              Export (Source)
            </div>
            {exportItems.map((item) => (
              <ProgressCard key={item.id} item={item} now={now} />
            ))}
          </div>

          {hasTargets && (
            <>
              <Divider className={styles.divider} />

              {/* Phase 2: Import (per target) */}
              <div className={styles.phaseSection}>
                <div className={styles.phaseHeader}>
                  <ArrowDownloadRegular className={styles.phaseIcon} />
                  Import (Targets)
                </div>
                {Array.from(targetGroups.entries()).map(([target, group]) => (
                  <div key={`import-${target}`} className={styles.targetGroup}>
                    <Text className={styles.targetLabel}>{target}</Text>
                    {group.imports.map((item) => (
                      <ProgressCard key={item.id} item={item} now={now} />
                    ))}
                  </div>
                ))}
              </div>

              <Divider className={styles.divider} />

              {/* Phase 3: Publish (per target) */}
              <div className={styles.phaseSection}>
                <div className={styles.phaseHeader}>
                  <CloudArrowUpRegular className={styles.phaseIcon} />
                  Publish (Targets)
                </div>
                {Array.from(targetGroups.entries()).map(([target, group]) => (
                  <div key={`publish-${target}`} className={styles.targetGroup}>
                    <Text className={styles.targetLabel}>{target}</Text>
                    {group.publishes.length > 0 ? (
                      group.publishes.map((item) => (
                        <ProgressCard key={item.id} item={item} now={now} />
                      ))
                    ) : (
                      <Text size={200} style={{ color: tokens.colorNeutralForeground4, padding: '4px 8px' }}>
                        Waiting for import...
                      </Text>
                    )}
                  </div>
                ))}
              </div>
            </>
          )}
        </>
      )}
    </Panel>
  );
}
