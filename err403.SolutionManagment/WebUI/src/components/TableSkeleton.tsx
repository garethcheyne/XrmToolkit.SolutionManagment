import {
  Skeleton, SkeletonItem,
  makeStyles, tokens,
} from '@fluentui/react-components';

const useStyles = makeStyles({
  root: {
    display: 'flex',
    flexDirection: 'column',
    flex: 1,
    padding: '0',
  },
  headerRow: {
    display: 'flex',
    gap: '16px',
    padding: '10px 12px',
    borderBottom: `2px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground2,
  },
  row: {
    display: 'flex',
    gap: '16px',
    padding: '8px 12px',
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
  },
});

interface TableSkeletonProps {
  columns?: number;
  rows?: number;
}

export function TableSkeleton({ columns = 5, rows = 12 }: TableSkeletonProps) {
  const styles = useStyles();
  const colWidths = ['20%', '25%', '12%', '15%', '15%', '13%'];

  return (
    <div className={styles.root}>
      <Skeleton>
        <div className={styles.headerRow}>
          {Array.from({ length: columns }).map((_, i) => (
            <SkeletonItem key={`h${i}`} size={16} style={{ width: colWidths[i % colWidths.length] ?? '15%' }} />
          ))}
        </div>
        {Array.from({ length: rows }).map((_, r) => (
          <div key={`r${r}`} className={styles.row}>
            {Array.from({ length: columns }).map((_, c) => (
              <SkeletonItem key={`c${c}`} size={12} style={{ width: colWidths[c % colWidths.length] ?? '15%' }} />
            ))}
          </div>
        ))}
      </Skeleton>
    </div>
  );
}
