import {
  Dialog, DialogSurface, DialogTitle, DialogBody,
  DialogContent, DialogActions,
  Button, Text, Link, Divider,
  makeStyles, tokens,
} from '@fluentui/react-components';

const useStyles = makeStyles({
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: '12px',
  },
  links: {
    display: 'flex',
    flexDirection: 'column',
    gap: '4px',
  },
  version: {
    color: tokens.colorNeutralForeground3,
    fontSize: '11px',
  },
});

interface AboutDialogProps {
  open: boolean;
  onClose: () => void;
}

export function AboutDialog({ open, onClose }: AboutDialogProps) {
  const styles = useStyles();

  return (
    <Dialog open={open} onOpenChange={(_e, data) => { if (!data.open) onClose(); }}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>Solution Management</DialogTitle>
          <DialogContent className={styles.content}>
            <Text>XrmToolBox plugin for managing Dataverse solutions, environment variables, cloud flows, and platform settings across multiple environments.</Text>

            <Divider />

            <Text weight="semibold">Features</Text>
            <Text size={200}>
              Transfer solutions between environments with full configuration control.
              Manage cloud flows, environment variables, and platform settings.
              Compare versions across source and target environments.
            </Text>

            <Divider />

            <div className={styles.links}>
              <Text weight="semibold">Links</Text>
              <Link href="#" onClick={(e) => { e.preventDefault(); import('../bridge').then(b => b.postMessage({ action: 'openUrl', url: 'https://github.com/garethcheyne/SolutionTransferTool' })); }}>
                GitHub Repository
              </Link>
              <Link href="#" onClick={(e) => { e.preventDefault(); import('../bridge').then(b => b.postMessage({ action: 'openUrl', url: 'https://github.com/garethcheyne/SolutionTransferTool/wiki' })); }}>
                Documentation
              </Link>
              <Link href="#" onClick={(e) => { e.preventDefault(); import('../bridge').then(b => b.postMessage({ action: 'openUrl', url: 'https://github.com/MscrmTools/DamSim.SolutionTransferTool' })); }}>
                Original Fork (MscrmTools)
              </Link>
            </div>

            <Divider />

            <Text className={styles.version}>
              Built with React + Fluent UI v9 + Vite + WebView2
            </Text>
            <Text className={styles.version}>
              By Gareth Cheyne (err403)
            </Text>
          </DialogContent>
          <DialogActions>
            <Button appearance="primary" onClick={onClose}>Close</Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
