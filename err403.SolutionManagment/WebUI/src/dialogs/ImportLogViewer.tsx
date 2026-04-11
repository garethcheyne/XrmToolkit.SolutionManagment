import {
  Dialog, DialogSurface, DialogTitle, DialogBody,
  DialogContent, DialogActions,
  Button, TabList, Tab, Textarea,
  makeStyles, type SelectTabData,
} from '@fluentui/react-components';
import { useState } from 'react';

const useStyles = makeStyles({
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: '12px',
    minHeight: '400px',
  },
  textArea: {
    fontFamily: 'Consolas, monospace',
    fontSize: '12px',
    minHeight: '350px',
  },
});

interface ImportLogViewerProps {
  message: string;
  rawXml: string;
  open: boolean;
  onClose: () => void;
}

export function ImportLogViewer({ message, rawXml, open, onClose }: ImportLogViewerProps) {
  const styles = useStyles();
  const [tab, setTab] = useState('message');

  return (
    <Dialog open={open} onOpenChange={(_e, data) => { if (!data.open) onClose(); }}>
      <DialogSurface style={{ maxWidth: '800px', minWidth: '600px' }}>
        <DialogBody>
          <DialogTitle>Import Log</DialogTitle>
          <DialogContent className={styles.content}>
            <TabList selectedValue={tab} onTabSelect={(_e: unknown, data: SelectTabData) => setTab(data.value as string)} size="small">
              <Tab value="message">Message</Tab>
              <Tab value="raw">Raw XML</Tab>
            </TabList>

            {tab === 'message' && (
              <Textarea
                className={styles.textArea}
                value={message || 'No message available.'}
                readOnly
                resize="vertical"
              />
            )}

            {tab === 'raw' && (
              <Textarea
                className={styles.textArea}
                value={rawXml || 'No XML data available.'}
                readOnly
                resize="vertical"
              />
            )}
          </DialogContent>
          <DialogActions>
            <Button appearance="secondary" onClick={() => {
              navigator.clipboard.writeText(tab === 'message' ? message : rawXml);
            }}>Copy</Button>
            <Button appearance="primary" onClick={onClose}>Close</Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
