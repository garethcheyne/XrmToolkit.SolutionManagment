import {
  Dialog, DialogSurface, DialogTitle, DialogBody,
  DialogContent, DialogActions,
  Button, Badge, Text,
  makeStyles,
} from '@fluentui/react-components';
import type { TransferResult } from '../types';
import { ResultsTable } from '../components/ResultsTable';
import { formatElapsed } from '../cards/ProgressCard';

const useStyles = makeStyles({
  surface: { maxWidth: '720px', width: '90vw' },
  summary: { display: 'flex', gap: '8px', padding: '8px 0', alignItems: 'center' },
  totalTime: { marginLeft: 'auto', fontSize: '12px' },
});

interface TransferResultsDialogProps {
  results: TransferResult[];
  open: boolean;
  onClose: () => void;
}

export function TransferResultsDialog({ results, open, onClose }: TransferResultsDialogProps) {
  const styles = useStyles();
  const successes = results.filter((r) => r.success).length;
  const failures = results.filter((r) => !r.success).length;

  const rows = results.map(r => ({
    success: r.success,
    name: r.solution,
    target: r.target,
    detail: r.error,
    elapsed: r.elapsedMs != null ? formatElapsed(r.elapsedMs) : undefined,
  }));

  // Total elapsed = max elapsed across all results (parallel imports)
  const totalMs = results.reduce((max, r) => Math.max(max, r.elapsedMs ?? 0), 0);

  return (
    <Dialog open={open} onOpenChange={(_e, data) => { if (!data.open) onClose(); }}>
      <DialogSurface className={styles.surface}>
        <DialogBody>
          <DialogTitle>Transfer Results</DialogTitle>
          <DialogContent>
            <div className={styles.summary}>
              <Badge color="success" appearance="filled" size="small">{successes} succeeded</Badge>
              {failures > 0 && <Badge color="danger" appearance="filled" size="small">{failures} failed</Badge>}
              {totalMs > 0 && (
                <Text className={styles.totalTime} weight="semibold">
                  Total: {formatElapsed(totalMs)}
                </Text>
              )}
            </div>
            <ResultsTable rows={rows} />
          </DialogContent>
          <DialogActions>
            <Button appearance="primary" onClick={onClose}>Close</Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
