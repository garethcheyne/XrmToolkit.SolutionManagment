import {
  Dialog, DialogSurface, DialogTitle, DialogBody,
  DialogContent, DialogActions,
  Button, Text, Badge,
  makeStyles, tokens,
} from '@fluentui/react-components';
import {
  CheckmarkCircleFilled, DismissCircleFilled,
} from '@fluentui/react-icons';
import type { TransferResult } from '../types';

const useStyles = makeStyles({
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: '4px',
    maxHeight: '400px',
    overflow: 'auto',
  },
  row: {
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
    padding: '6px 8px',
    borderRadius: '4px',
  },
  successRow: {
    backgroundColor: tokens.colorPaletteGreenBackground1,
  },
  errorRow: {
    backgroundColor: tokens.colorPaletteRedBackground1,
  },
  summary: {
    padding: '8px 0',
  },
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

  return (
    <Dialog open={open} onOpenChange={(_e, data) => { if (!data.open) onClose(); }}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>Transfer Results</DialogTitle>
          <DialogContent>
            <div className={styles.summary}>
              <Badge color="success" appearance="filled" size="small">{successes} succeeded</Badge>
              {' '}
              {failures > 0 && <Badge color="danger" appearance="filled" size="small">{failures} failed</Badge>}
            </div>
            <div className={styles.content}>
              {results.map((r, i) => (
                <div key={i} className={`${styles.row} ${r.success ? styles.successRow : styles.errorRow}`}>
                  {r.success
                    ? <CheckmarkCircleFilled color={tokens.colorPaletteGreenForeground1} fontSize={18} />
                    : <DismissCircleFilled color={tokens.colorPaletteRedForeground1} fontSize={18} />
                  }
                  <Text size={200} weight="semibold">{r.solution}</Text>
                  <Text size={200}>→ {r.target}</Text>
                  {!r.success && <Text size={200} style={{ color: tokens.colorPaletteRedForeground1 }}>{r.error}</Text>}
                </div>
              ))}
            </div>
          </DialogContent>
          <DialogActions>
            <Button appearance="primary" onClick={onClose}>Close</Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
