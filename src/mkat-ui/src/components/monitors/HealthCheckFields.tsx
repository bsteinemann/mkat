import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';

export interface HealthCheckValues {
  healthCheckUrl: string;
  httpMethod: string;
  expectedStatusCodes: string;
  timeoutSeconds: number;
  bodyMatchRegex: string;
}

interface Props {
  values: HealthCheckValues;
  onChange: (field: keyof HealthCheckValues, value: string | number | undefined) => void;
  urlRequired?: boolean;
}

export function HealthCheckFields({ values, onChange, urlRequired }: Props) {
  return (
    <div className="space-y-3 border-t pt-3 mt-3">
      <div className="space-y-1">
        <Label className="text-xs text-muted-foreground">URL</Label>
        <Input
          type="url"
          value={values.healthCheckUrl}
          onChange={e => onChange('healthCheckUrl', e.target.value || undefined)}
          className="h-8 text-sm"
          placeholder="https://example.com/health"
          required={urlRequired}
        />
      </div>
      <div className="grid grid-cols-2 gap-3">
        <div className="space-y-1">
          <Label className="text-xs text-muted-foreground">HTTP Method</Label>
          <Select value={values.httpMethod} onValueChange={v => onChange('httpMethod', v)}>
            <SelectTrigger className="w-full h-8 text-sm" size="sm">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="GET">GET</SelectItem>
              <SelectItem value="HEAD">HEAD</SelectItem>
              <SelectItem value="POST">POST</SelectItem>
              <SelectItem value="PUT">PUT</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-1">
          <Label className="text-xs text-muted-foreground">Timeout (seconds)</Label>
          <Input
            type="number"
            value={values.timeoutSeconds}
            onChange={e => onChange('timeoutSeconds', Number(e.target.value))}
            className="h-8 text-sm"
            min={1}
            max={120}
          />
        </div>
      </div>
      <div className="space-y-1">
        <Label className="text-xs text-muted-foreground">Expected Status Codes</Label>
        <Input
          type="text"
          value={values.expectedStatusCodes}
          onChange={e => onChange('expectedStatusCodes', e.target.value)}
          className="h-8 text-sm"
          placeholder="200,201,204"
        />
      </div>
      <div className="space-y-1">
        <Label className="text-xs text-muted-foreground">Body Match Regex (optional)</Label>
        <Input
          type="text"
          value={values.bodyMatchRegex}
          onChange={e => onChange('bodyMatchRegex', e.target.value || undefined)}
          className="h-8 text-sm"
          placeholder="ok|healthy"
        />
      </div>
    </div>
  );
}
