import { useState, useMemo } from 'react';
import {
  Dialog, DialogSurface, DialogTitle, DialogBody,
  DialogContent, DialogActions,
  Button, Text, Badge, ProgressBar,
  makeStyles, tokens,
} from '@fluentui/react-components';
import {
  WarningFilled, DismissRegular,
  ArrowClockwiseRegular, SkipForward10Regular,
} from '@fluentui/react-icons';
import { postMessage } from '../bridge';

export interface ActiveImportInfo {
  solutionName: string;
  startedOn: string;
  progress: number;
  createdBy: string;
}

export interface ActiveImportsDialogProps {
  open: boolean;
  /** Keyed by target connection name */
  activeImports: Record<string, ActiveImportInfo[]>;
  onClose: () => void;
}

type Decision = 'skip' | 'wait' | undefined;

const useStyles = makeStyles({
  surface: {
    maxWidth: '680px',
    width: '680px',
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: '16px',
  },
  warningBanner: {
    display: 'flex',
    gap: '12px',
    alignItems: 'flex-start',
    padding: '12px',
    backgroundColor: tokens.colorPaletteYellowBackground1,
    borderRadius: tokens.borderRadiusMedium,
  },
  warningIcon: {
    flexShrink: 0,
    color: tokens.colorPaletteYellowForeground2,
    fontSize: '20px',
    marginTop: '2px',
  },
  targetSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: '8px',
    padding: '12px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    borderLeft: `3px solid ${tokens.colorPaletteYellowBorder2}`,
  },
  targetHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  targetName: {
    fontWeight: 600,
  },
  decisionButtons: {
    display: 'flex',
    gap: '6px',
  },
  importRow: {
    display: 'flex',
    flexDirection: 'column',
    gap: '4px',
    padding: '8px',
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusSmall,
  },
  importHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  importMeta: {
    fontSize: '11px',
    color: tokens.colorNeutralForeground3,
  },
});

export function ActiveImportsDialog({ open, activeImports, onClose }: ActiveImportsDialogProps) {
  const styles = useStyles();
  const targetNames = useMemo(() => Object.keys(activeImports), [activeImports]);
  const [decisions, setDecisions] = useState<Record<string, Decision>>({});

  const setDecision = (target: string, decision: Decision) => {
    setDecisions((prev) => ({ ...prev, [target]: decision }));
  };

  const allDecided = targetNames.length > 0 && targetNames.every((t) => decisions[t] != null);

  const handleConfirm = () => {
    const skipTargets: string[] = [];
    const waitTargets: string[] = [];
    for (const target of targetNames) {
      if (decisions[target] === 'skip') skipTargets.push(target);
      else if (decisions[target] === 'wait') waitTargets.push(target);
    }
    postMessage({ action: 'activeImportsResponse', skipTargets, waitTargets });
    onClose();
  };

  const handleCancel = () => {
    // Don't send a response — C# will just leave _pendingTransfer unused
    onClose();
  };

  const formatTime = (iso: string) => {
    try {
      const d = new Date(iso);
      return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    } catch {
      return iso;
    }
  };

  return (
    <Dialog open={open} onOpenChange={(_, data) => { if (!data.open) handleCancel(); }}>
      <DialogSurface className={styles.surface}>
        <DialogTitle>Active Imports Detected</DialogTitle>
        <DialogBody>
          <DialogContent className={styles.content}>
            <div className={styles.warningBanner}>
              <WarningFilled className={styles.warningIcon} />
              <Text size={200}>
                The following targets have imports currently in progress.
                Choose to <strong>Wait</strong> (poll until complete then proceed)
                or <strong>Skip</strong> (exclude target from this transfer) for each.
              </Text>
            </div>

            {targetNames.map((target) => (
              <div key={target} className={styles.targetSection}>
                <div className={styles.targetHeader}>
                  <Text className={styles.targetName}>{target}</Text>
                  <div className={styles.decisionButtons}>
                    <Button
                      size="small"
                      appearance={decisions[target] === 'wait' ? 'primary' : 'outline'}
                      icon={<ArrowClockwiseRegular />}
                      onClick={() => setDecision(target, 'wait')}
                    >
                      Wait
                    </Button>
                    <Button
                      size="small"
                      appearance={decisions[target] === 'skip' ? 'primary' : 'outline'}
                      icon={<SkipForward10Regular />}
                      onClick={() => setDecision(target, 'skip')}
                    >
                      Skip
                    </Button>
                  </div>
                </div>

                {(activeImports[target] ?? []).map((imp, idx) => (
                  <div key={idx} className={styles.importRow}>
                    <div className={styles.importHeader}>
                      <Text weight="semibold" size={200}>{imp.solutionName}</Text>
                      <Badge appearance="outline" size="small" color="warning">
                        {Math.round(imp.progress)}%
                      </Badge>
                    </div>
                    <ProgressBar
                      value={imp.progress / 100}
                      thickness="large"
                      color="warning"
                    />
                    <Text className={styles.importMeta}>
                      Started {formatTime(imp.startedOn)} by {imp.createdBy}
                    </Text>
                  </div>
                ))}
              </div>
            ))}
          </DialogContent>
          <DialogActions>
            <Button appearance="secondary" icon={<DismissRegular />} onClick={handleCancel}>
              Cancel Transfer
            </Button>
            <Button appearance="primary" disabled={!allDecided} onClick={handleConfirm}>
              Proceed
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
