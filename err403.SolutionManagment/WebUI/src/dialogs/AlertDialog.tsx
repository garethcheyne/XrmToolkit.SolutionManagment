import {
  Dialog, DialogSurface, DialogTitle, DialogBody,
  DialogContent, DialogActions,
  Button, Text,
  makeStyles, tokens,
} from '@fluentui/react-components';
import {
  CheckmarkCircleFilled, ErrorCircleFilled, InfoFilled, WarningFilled,
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
    whiteSpace: 'pre-wrap',
    wordBreak: 'break-word',
    maxHeight: '300px',
    overflow: 'auto',
  },
});

export type AlertSeverity = 'success' | 'error' | 'warning' | 'info';

export interface AlertDialogState {
  open: boolean;
  title: string;
  message: string;
  severity: AlertSeverity;
}

export const emptyAlert: AlertDialogState = {
  open: false, title: '', message: '', severity: 'info',
};

interface AlertDialogProps {
  state: AlertDialogState;
  onClose: () => void;
}

export function AlertDialog({ state, onClose }: AlertDialogProps) {
  const styles = useStyles();

  const iconMap = {
    success: <CheckmarkCircleFilled fontSize={24} color={tokens.colorPaletteGreenForeground1} />,
    error: <ErrorCircleFilled fontSize={24} color={tokens.colorPaletteRedForeground1} />,
    warning: <WarningFilled fontSize={24} color={tokens.colorPaletteYellowForeground1} />,
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
            <Button appearance="primary" onClick={onClose}>OK</Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
