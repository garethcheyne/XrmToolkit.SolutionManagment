import {
  Dialog, DialogSurface, DialogTitle, DialogBody,
  DialogContent, DialogActions,
  Button, Badge,
  makeStyles,
} from '@fluentui/react-components';
import type { FlowResult } from '../types';
import { ResultsTable } from '../components/ResultsTable';

const useStyles = makeStyles({
  surface: { maxWidth: '720px', width: '90vw' },
  summary: { display: 'flex', gap: '8px', padding: '8px 0' },
});

interface FlowResultsDialogProps {
  results: FlowResult[];
  open: boolean;
  onClose: () => void;
}

export function FlowResultsDialog({ results, open, onClose }: FlowResultsDialogProps) {
  const styles = useStyles();
  const successes = results.filter(r => r.success).length;
  const failures = results.filter(r => !r.success).length;

  const rows = results.map(r => ({
    success: r.success ?? r.Success ?? false,
    name: r.flowName ?? r.FlowName ?? '',
    target: r.targetName ?? r.TargetName ?? '',
    detail: r.errorMessage ?? r.ErrorMessage,
  }));

  return (
    <Dialog open={open} onOpenChange={(_e, data) => { if (!data.open) onClose(); }}>
      <DialogSurface className={styles.surface}>
        <DialogBody>
          <DialogTitle>Flow Activation Results</DialogTitle>
          <DialogContent>
            <div className={styles.summary}>
              <Badge color="success" appearance="filled" size="small">{successes} succeeded</Badge>
              {failures > 0 && <Badge color="danger" appearance="filled" size="small">{failures} failed</Badge>}
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
