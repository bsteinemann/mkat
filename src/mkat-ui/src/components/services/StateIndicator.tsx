import { ServiceState } from '../../api/types';

interface Props {
  state: ServiceState;
  size?: 'sm' | 'md' | 'lg';
}

const stateConfig = {
  [ServiceState.Up]: { color: 'bg-green-500', label: 'Up', pulse: false },
  [ServiceState.Down]: { color: 'bg-red-500', label: 'Down', pulse: true },
  [ServiceState.Paused]: { color: 'bg-yellow-500', label: 'Paused', pulse: false },
  [ServiceState.Unknown]: { color: 'bg-gray-400', label: 'Unknown', pulse: false },
};

const sizes = {
  sm: 'h-2 w-2',
  md: 'h-3 w-3',
  lg: 'h-4 w-4',
};

export function StateIndicator({ state, size = 'md' }: Props) {
  const config = stateConfig[state];

  return (
    <span className="flex items-center gap-2">
      <span className={`relative inline-flex ${sizes[size]}`}>
        {config.pulse && (
          <span className={`animate-ping absolute inline-flex h-full w-full rounded-full ${config.color} opacity-75`} />
        )}
        <span className={`relative inline-flex rounded-full ${sizes[size]} ${config.color}`} />
      </span>
      <span className="text-sm font-medium">{config.label}</span>
    </span>
  );
}
