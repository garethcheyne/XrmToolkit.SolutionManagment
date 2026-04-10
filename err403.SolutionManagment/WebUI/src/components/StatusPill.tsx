import { Badge } from '@fluentui/react-components';
import {
  CircleFilled,
} from '@fluentui/react-icons';

interface StatusPillProps {
  status: string;
  stateCode: number;
  isError?: boolean;
}

export function StatusPill({ status, stateCode, isError }: StatusPillProps) {
  if (isError) {
    return (
      <Badge appearance="filled" color="danger" icon={<CircleFilled />}>
        {status}
      </Badge>
    );
  }

  switch (stateCode) {
    case 1:
      return (
        <Badge appearance="filled" color="success" icon={<CircleFilled />}>
          {status}
        </Badge>
      );
    case 2:
      return (
        <Badge appearance="filled" color="warning" icon={<CircleFilled />}>
          {status}
        </Badge>
      );
    case 0:
      return (
        <Badge appearance="tint" color="informative" icon={<CircleFilled />}>
          {status}
        </Badge>
      );
    default:
      return (
        <Badge appearance="tint" color="informative">
          {status}
        </Badge>
      );
  }
}
