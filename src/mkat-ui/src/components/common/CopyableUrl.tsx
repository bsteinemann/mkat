import { useState } from 'react';
import { ClipboardIcon, CheckIcon } from '@heroicons/react/24/outline';

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
      <label className="text-sm font-medium text-gray-700">{label}</label>
      <div className="flex items-center gap-2">
        <code className="flex-1 bg-gray-100 px-3 py-2 rounded text-sm font-mono truncate">
          {url}
        </code>
        <button
          onClick={handleCopy}
          className="p-2 text-gray-500 hover:text-gray-700 rounded hover:bg-gray-100"
          title={copied ? 'Copied!' : 'Copy to clipboard'}
        >
          {copied ? (
            <CheckIcon className="h-5 w-5 text-green-500" />
          ) : (
            <ClipboardIcon className="h-5 w-5" />
          )}
        </button>
      </div>
    </div>
  );
}
