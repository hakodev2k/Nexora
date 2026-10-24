import { useState } from 'react';
import { requestDirtyLeave } from './App';
import { CalendarImportScreen } from './CalendarImportScreen';
import { CalendarExportScreen } from './CalendarExportScreen';

export function CalendarTransferScreen({ onAuthLost }: { onAuthLost: () => void }) {
  const [mode, setMode] = useState<'import' | 'export'>('import');
  return <><nav className="module-tabs" aria-label="Calendar transfer">
    <button className={mode === 'import' ? 'tab-button active' : 'tab-button'} aria-current={mode === 'import' ? 'page' : undefined} onClick={() => requestDirtyLeave(() => setMode('import'))}>ICS Import</button>
    <button className={mode === 'export' ? 'tab-button active' : 'tab-button'} aria-current={mode === 'export' ? 'page' : undefined} onClick={() => requestDirtyLeave(() => setMode('export'))}>ICS Export</button>
  </nav>{mode === 'import' ? <CalendarImportScreen onAuthLost={onAuthLost} /> : <CalendarExportScreen onAuthLost={onAuthLost} />}</>;
}
