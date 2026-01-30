import { useState } from 'react';
import { ClipboardIcon, CheckIcon } from '@heroicons/react/24/outline';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';

interface Props {
  label: string;
  url: string;
}

export function CopyableUrl({ label, url }: Props) {
  const [copied, setCopied] = useState(false);

  const handleCopy = async () => {
    await navigator.clipboard.writeText(url);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className="flex flex-col gap-1">
      <Label className="text-foreground">{label}</Label>
      <div className="flex items-center gap-2">
        <code className="flex-1 bg-muted px-3 py-2 rounded text-sm font-mono truncate">{url}</code>
        <TooltipProvider>
          <Tooltip>
            <TooltipTrigger asChild>
              <Button variant="ghost" size="icon" onClick={handleCopy}>
                {copied ? (
                  <CheckIcon className="h-5 w-5 text-green-500" />
                ) : (
                  <ClipboardIcon className="h-5 w-5" />
                )}
              </Button>
            </TooltipTrigger>
            <TooltipContent>
              <p>{copied ? 'Copied!' : 'Copy to clipboard'}</p>
            </TooltipContent>
          </Tooltip>
        </TooltipProvider>
      </div>
    </div>
  );
}
