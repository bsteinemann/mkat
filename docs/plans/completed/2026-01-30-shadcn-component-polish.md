# shadcn/ui Component Polish Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace remaining hand-built UI patterns with shadcn/ui components for a more polished, consistent frontend.

**Architecture:** Each task swaps one pattern type across all files. No new features — only component upgrades. The app stays functional after every task.

**Tech Stack:** React 19, shadcn/ui, Tailwind CSS v4, Vite 7

---

## Task 1: Skeleton Loading States

Replace all `<div>Loading...</div>` with skeleton loaders that match the page layout.

**Files:**
- Modify: `src/mkat-ui/src/pages/Services.tsx` (line 28)
- Modify: `src/mkat-ui/src/pages/Alerts.tsx` (line 22)
- Modify: `src/mkat-ui/src/pages/ServiceDetail.tsx` (line 58)
- Modify: `src/mkat-ui/src/pages/ServiceEdit.tsx` (line 94, lines 487-498)
- Modify: `src/mkat-ui/src/pages/Contacts.tsx` (line 46)
- Modify: `src/mkat-ui/src/pages/Peers.tsx` (line 44)

**What to do:**

The `Skeleton` component is already installed at `src/mkat-ui/src/components/ui/skeleton.tsx`. Import it with `import { Skeleton } from '@/components/ui/skeleton';`.

For each page, replace `<div>Loading...</div>` with skeletons that approximate the page structure:

- **Services**: Title skeleton + 3 card skeletons (h-24 rounded-lg)
- **Alerts**: Title skeleton + 3 alert item skeletons (h-16 rounded)
- **ServiceDetail**: Title + state indicator skeleton, then 2 card skeletons
- **ServiceEdit**: Title skeleton + card skeleton (h-48) + monitor section skeleton
- **Contacts**: Title skeleton + 3 card skeletons (h-16)
- **Peers**: Title skeleton + 2 card skeletons (h-20)
- **ServiceEdit ContactsSection** (lines 487-498): Replace `<p>Loading...</p>` inside the card with 3 small skeleton rows

Pattern for each page:
```tsx
if (isLoading) return (
  <div className="space-y-6">
    <Skeleton className="h-8 w-48" />
    <div className="space-y-4">
      <Skeleton className="h-24 w-full rounded-lg" />
      <Skeleton className="h-24 w-full rounded-lg" />
      <Skeleton className="h-24 w-full rounded-lg" />
    </div>
  </div>
);
```

Adjust sizes to match each page's actual layout.

**Verify:** `cd src/mkat-ui && npm run build`

**Commit:** `refactor: replace Loading text with Skeleton loaders`

---

## Task 2: Tooltip on CopyableUrl

Add a tooltip to the copy button in CopyableUrl showing "Copy to clipboard" / "Copied!".

**Files:**
- Modify: `src/mkat-ui/src/components/common/CopyableUrl.tsx`

**What to do:**

The `Tooltip` component is already installed at `src/mkat-ui/src/components/ui/tooltip.tsx`. Import `Tooltip`, `TooltipContent`, `TooltipProvider`, `TooltipTrigger`.

Wrap the copy button:
```tsx
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';

// In the JSX:
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
```

Remove the `title` prop from the Button (tooltip replaces it).

**Verify:** `cd src/mkat-ui && npm run build`

**Commit:** `refactor: add Tooltip to CopyableUrl copy button`

---

## Task 3: Switch for Channel Enable/Disable

Replace the "Enable"/"Disable" button toggle in Contacts with a shadcn Switch.

**Files:**
- Add: `npx shadcn@latest add switch`
- Modify: `src/mkat-ui/src/pages/Contacts.tsx` (ContactDetail component, around line 308-325)

**What to do:**

Currently each channel row has:
```tsx
<Badge variant="outline" className={...}>{ch.isEnabled ? 'Enabled' : 'Disabled'}</Badge>
// and
<Button variant="outline" size="xs" onClick={() => toggleChannelMutation.mutate(ch)}>
  {ch.isEnabled ? 'Disable' : 'Enable'}
</Button>
```

Replace both the Badge and the Button with a single Switch:
```tsx
import { Switch } from '@/components/ui/switch';

// Replace the badge + button with:
<div className="flex items-center gap-2">
  <Switch
    checked={ch.isEnabled}
    onCheckedChange={() => toggleChannelMutation.mutate(ch)}
  />
  <span className={`text-xs ${ch.isEnabled ? 'text-green-600' : 'text-gray-400'}`}>
    {ch.isEnabled ? 'Enabled' : 'Disabled'}
  </span>
</div>
```

Remove the old `<Badge>` for enabled/disabled status and the `<Button>` for toggle. Keep the Test and Remove buttons.

**Verify:** `cd src/mkat-ui && npm run build`

**Commit:** `refactor: use Switch for channel enable/disable toggle`

---

## Task 4: Alert Component for Login Error and Empty States

Replace the hand-built error div on Login and standardize empty states with the shadcn Alert component.

**Files:**
- Add: `npx shadcn@latest add alert`
- Modify: `src/mkat-ui/src/pages/Login.tsx` (lines 41-44)
- Modify: `src/mkat-ui/src/pages/Services.tsx` (lines 42-48)
- Modify: `src/mkat-ui/src/pages/Alerts.tsx` (lines 28-31)
- Modify: `src/mkat-ui/src/pages/Contacts.tsx` (lines 68-73)
- Modify: `src/mkat-ui/src/pages/Peers.tsx` (lines 59-64)
- Modify: `src/mkat-ui/src/pages/Dashboard.tsx` (line 49 — "No recent alerts")
- Modify: `src/mkat-ui/src/pages/ServiceDetail.tsx` (line 211 — "No alerts for this service")
- Modify: `src/mkat-ui/src/pages/ServiceEdit.tsx` (line 509 — "No contacts configured yet")

**What to do:**

1. **Login error** — replace:
   ```tsx
   <div className="bg-red-100 text-red-700 p-3 rounded mb-4">{error}</div>
   ```
   with:
   ```tsx
   import { Alert, AlertDescription } from '@/components/ui/alert';
   import { AlertCircle } from 'lucide-react';

   <Alert variant="destructive">
     <AlertCircle className="h-4 w-4" />
     <AlertDescription>{error}</AlertDescription>
   </Alert>
   ```

2. **Empty states** — replace plain text empty states with:
   ```tsx
   import { Alert, AlertDescription } from '@/components/ui/alert';
   import { Info } from 'lucide-react';

   <Alert>
     <Info className="h-4 w-4" />
     <AlertDescription>No services configured yet.</AlertDescription>
   </Alert>
   ```

   For empty states that currently live inside a Card (like Contacts, Peers), replace the Card with an Alert directly. For empty states that are inline text (`<p className="text-gray-500">`), wrap in Alert.

3. For the Services page empty state that has a "Create your first service" link, keep the link inside the AlertDescription.

**Verify:** `cd src/mkat-ui && npm run build`

**Commit:** `refactor: use Alert component for errors and empty states`

---

## Task 5: Tabs for Peer Dialog Modes

Replace the mode-based state (`choose` | `generate` | `enter`) in PairDialog with shadcn Tabs.

**Files:**
- Add: `npx shadcn@latest add tabs`
- Modify: `src/mkat-ui/src/pages/Peers.tsx` (PairDialog function, lines 121-252)

**What to do:**

Currently PairDialog has:
```tsx
const [mode, setMode] = useState<'choose' | 'generate' | 'enter'>('choose');
```
with three conditional renders and a "choose" screen with two buttons.

Replace with Tabs:
```tsx
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';

// Remove the mode state. Replace the mode-based rendering with:
<Tabs defaultValue="generate" className="w-full">
  <TabsList className="grid w-full grid-cols-2">
    <TabsTrigger value="generate">Generate Token</TabsTrigger>
    <TabsTrigger value="enter">Enter Token</TabsTrigger>
  </TabsList>
  <TabsContent value="generate">
    {/* content from mode === 'generate' */}
  </TabsContent>
  <TabsContent value="enter">
    {/* content from mode === 'enter' */}
  </TabsContent>
</Tabs>
```

This removes the "choose" step entirely — users see both options as tabs and click the one they want. Simpler UX.

Remove the `mode` state, the `setMode` calls, and the "choose" conditional block. Move the generate and enter content into their respective TabsContent.

The generate tab needs to handle both states (before and after token generation) — keep the existing conditional for `generatedToken`.

**Verify:** `cd src/mkat-ui && npm run build`

**Commit:** `refactor: use Tabs for peer pairing dialog modes`

---

## Task 6: DropdownMenu for Action Buttons

Replace inline button groups with DropdownMenu where there are 3+ actions on a single item.

**Files:**
- Add: `npx shadcn@latest add dropdown-menu`
- Modify: `src/mkat-ui/src/pages/Contacts.tsx` (channel actions — Test, Enable/Disable, Remove)
- Modify: `src/mkat-ui/src/pages/ServiceDetail.tsx` (service actions — Pause/Resume, Edit)

**What to do:**

1. **Contacts — channel actions** (lines 312-346): Currently Test + Switch + Remove buttons inline. Replace with a DropdownMenu:
   ```tsx
   import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger, DropdownMenuSeparator } from '@/components/ui/dropdown-menu';
   import { MoreHorizontal } from 'lucide-react';

   <DropdownMenu>
     <DropdownMenuTrigger asChild>
       <Button variant="ghost" size="icon" className="h-8 w-8">
         <MoreHorizontal className="h-4 w-4" />
       </Button>
     </DropdownMenuTrigger>
     <DropdownMenuContent align="end">
       <DropdownMenuItem onClick={() => testChannelMutation.mutate(ch.id)}>
         Send Test
       </DropdownMenuItem>
       <DropdownMenuSeparator />
       <DropdownMenuItem
         className="text-red-600"
         onClick={() => deleteChannelMutation.mutate(ch.id)}
       >
         Remove
       </DropdownMenuItem>
     </DropdownMenuContent>
   </DropdownMenu>
   ```

   Keep the Switch for enable/disable separate from the dropdown (it's a toggle, not an action). The layout becomes: channel info + Switch + DropdownMenu.

   NOTE: The Remove action previously used AlertDialog. Since DropdownMenuItem can't directly contain AlertDialog (Radix portal conflicts), use a controlled AlertDialog pattern:
   - Add state: `const [channelToRemove, setChannelToRemove] = useState<string | null>(null)`
   - DropdownMenuItem onClick sets `channelToRemove`
   - Render a single `<AlertDialog open={!!channelToRemove} onOpenChange={() => setChannelToRemove(null)}>` outside the map loop

2. **ServiceDetail — service actions** (lines 69-97): Pause/Resume + Edit. Only 2 actions — keep as buttons (dropdown not worth it for 2 items).

**Verify:** `cd src/mkat-ui && npm run build`

**Commit:** `refactor: use DropdownMenu for channel actions`

---

## Task 7: shadcn Pagination Component

Replace the custom Pagination component with shadcn's built-in Pagination.

**Files:**
- Add: `npx shadcn@latest add pagination`
- Modify: `src/mkat-ui/src/components/common/Pagination.tsx` (rewrite to use shadcn)
- No changes needed to Services.tsx or Alerts.tsx (same props interface)

**What to do:**

Rewrite Pagination.tsx to use shadcn components while keeping the same Props interface:

```tsx
import {
  Pagination as ShadcnPagination,
  PaginationContent,
  PaginationItem,
  PaginationNext,
  PaginationPrevious,
} from '@/components/ui/pagination';

interface Props {
  page: number;
  totalCount: number;
  pageSize: number;
  onPageChange: (page: number) => void;
}

export function Pagination({ page, totalCount, pageSize, onPageChange }: Props) {
  const totalPages = Math.ceil(totalCount / pageSize);
  if (totalPages <= 1) return null;

  return (
    <div className="flex items-center justify-between pt-4">
      <span className="text-sm text-muted-foreground">
        {totalCount} total items
      </span>
      <ShadcnPagination>
        <PaginationContent>
          <PaginationItem>
            <PaginationPrevious
              onClick={() => onPageChange(page - 1)}
              className={page <= 1 ? 'pointer-events-none opacity-50' : 'cursor-pointer'}
            />
          </PaginationItem>
          <PaginationItem>
            <span className="px-3 py-1 text-sm">
              Page {page} of {totalPages}
            </span>
          </PaginationItem>
          <PaginationItem>
            <PaginationNext
              onClick={() => onPageChange(page + 1)}
              className={page >= totalPages ? 'pointer-events-none opacity-50' : 'cursor-pointer'}
            />
          </PaginationItem>
        </PaginationContent>
      </ShadcnPagination>
    </div>
  );
}
```

The Props interface stays identical, so Services.tsx and Alerts.tsx need no changes.

**Verify:** `cd src/mkat-ui && npm run build`

**Commit:** `refactor: use shadcn Pagination component`

---

## Summary

| Task | What | Files |
|------|------|-------|
| 1 | Skeleton loading states | 6 pages |
| 2 | Tooltip on copy button | 1 component |
| 3 | Switch for channel toggle | 1 page |
| 4 | Alert for errors + empty states | 8 pages |
| 5 | Tabs for peer dialog | 1 page |
| 6 | DropdownMenu for channel actions | 1 page |
| 7 | shadcn Pagination | 1 component |
