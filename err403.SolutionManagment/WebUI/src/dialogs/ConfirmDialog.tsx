import {
  Dialog, DialogSurface, DialogTitle, DialogBody,
  DialogContent, DialogActions,
  Button, Text,
  makeStyles, tokens,
} from '@fluentui/react-components';
import {
  WarningFilled, ErrorCircleFilled, InfoFilled,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  content: {
    display: 'flex',
    gap: '16px',
    alignItems: 'flex-start',
    maxWidth: '460px',
  },
  icon: {
    flexShrink: 0,
    marginTop: '2px',
  },
  body: {
    display: 'flex',
    flexDirection: 'column',
    gap: '8px',
  },
});

export type ConfirmDialogSeverity = 'warning' | 'danger' | 'info';

export interface ConfirmDialogState {
  open: boolean;
  title: string;
  message: string;
  severity: ConfirmDialogSeverity;
  confirmLabel?: string;
  onConfirm: () => void;
}

export const emptyConfirm: ConfirmDialogState = {
  open: false, title: '', message: '', severity: 'warning', onConfirm: () => {},
};

interface ConfirmDialogProps {
  state: ConfirmDialogState;
  onClose: () => void;
}

export function ConfirmDialog({ state, onClose }: ConfirmDialogProps) {
  const styles = useStyles();

  const iconMap = {
    warning: <WarningFilled fontSize={24} color={tokens.colorPaletteYellowForeground1} />,
    danger: <ErrorCircleFilled fontSize={24} color={tokens.colorPaletteRedForeground1} />,
    info: <InfoFilled fontSize={24} color={tokens.colorBrandForeground1} />,
  };

  return (
    <Dialog open={state.open} onOpenChange={(_e, data) => { if (!data.open) onClose(); }}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>{state.title}</DialogTitle>
          <DialogContent>
            <div className={styles.content}>
              <span className={styles.icon}>{iconMap[state.severity]}</span>
              <div className={styles.body}>
                <Text>{state.message}</Text>
              </div>
            </div>
          </DialogContent>
          <DialogActions>
            <Button appearance="secondary" onClick={onClose}>No</Button>
            <Button
              appearance={state.severity === 'danger' ? 'primary' : 'primary'}
              style={state.severity === 'danger' ? { backgroundColor: tokens.colorPaletteRedBackground3, borderColor: tokens.colorPaletteRedBackground3 } : undefined}
              onClick={() => { state.onConfirm(); onClose(); }}
            >
              {state.confirmLabel ?? 'Yes'}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
