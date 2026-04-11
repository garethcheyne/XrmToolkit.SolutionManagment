import {
  Dialog, DialogSurface, DialogTitle, DialogBody,
  DialogContent, DialogActions,
  Button, Text, Badge,
  DataGrid, DataGridHeader, DataGridRow, DataGridHeaderCell,
  DataGridBody, DataGridCell, createTableColumn,
  makeStyles, tokens,
} from '@fluentui/react-components';

const useStyles = makeStyles({
  content: { maxHeight: '500px', overflow: 'auto' },
  headerCell: { fontWeight: 700, fontSize: '12px', backgroundColor: tokens.colorNeutralBackground3 },
});

interface MissingComponent {
  requiredType: string;
  requiredName: string;
  requiredSchemaName: string;
  requiredSolution: string;
  dependentType: string;
  dependentName: string;
}

interface MissingDepsDialogProps {
  components: MissingComponent[];
  open: boolean;
  onClose: () => void;
}

const columns = [
  createTableColumn<MissingComponent>({ columnId: 'requiredType',
    renderHeaderCell: () => 'Required Type', renderCell: (item) => <Badge size="small" appearance="tint">{item.requiredType}</Badge> }),
  createTableColumn<MissingComponent>({ columnId: 'requiredName',
    renderHeaderCell: () => 'Required Component', renderCell: (item) => <Text size={200} weight="semibold">{item.requiredName}</Text> }),
  createTableColumn<MissingComponent>({ columnId: 'requiredSolution',
    renderHeaderCell: () => 'Solution', renderCell: (item) => item.requiredSolution }),
  createTableColumn<MissingComponent>({ columnId: 'dependentType',
    renderHeaderCell: () => 'Dependent Type', renderCell: (item) => item.dependentType }),
  createTableColumn<MissingComponent>({ columnId: 'dependentName',
    renderHeaderCell: () => 'Dependent', renderCell: (item) => item.dependentName }),
];

export function MissingDepsDialog({ components, open, onClose }: MissingDepsDialogProps) {
  const styles = useStyles();

  return (
    <Dialog open={open} onOpenChange={(_e, data) => { if (!data.open) onClose(); }}>
      <DialogSurface style={{ maxWidth: '800px' }}>
        <DialogBody>
          <DialogTitle>Missing Dependencies ({components.length})</DialogTitle>
          <DialogContent className={styles.content}>
            {components.length === 0 ? (
              <Text>No missing dependencies found.</Text>
            ) : (
              <DataGrid items={components} columns={columns} sortable size="small"
                getRowId={(item) => item.requiredSchemaName + item.dependentName} style={{ minWidth: '100%' }}>
                <DataGridHeader style={{ position: 'sticky', top: 0, zIndex: 1, backgroundColor: tokens.colorNeutralBackground3 }}>
                  <DataGridRow>{({ renderHeaderCell }) => <DataGridHeaderCell className={styles.headerCell}>{renderHeaderCell()}</DataGridHeaderCell>}</DataGridRow>
                </DataGridHeader>
                <DataGridBody<MissingComponent>>
                  {({ item, rowId }) => (
                    <DataGridRow<MissingComponent> key={rowId}>
                      {({ renderCell }) => <DataGridCell>{renderCell(item)}</DataGridCell>}
                    </DataGridRow>
                  )}
                </DataGridBody>
              </DataGrid>
            )}
          </DialogContent>
          <DialogActions>
            <Button appearance="primary" onClick={onClose}>Close</Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
