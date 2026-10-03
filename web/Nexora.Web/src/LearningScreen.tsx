import { useState } from 'react';
import { requestDirtyLeave } from './App';
import { SkillScreen } from './SkillScreen';
import { CourseScreen } from './CourseScreen';

export function LearningScreen({ onAuthLost }: { onAuthLost: () => void }) {
  const [tab, setTab] = useState<'Skills' | 'Courses'>('Skills');
  return <><nav aria-label="Learning sections">{(['Skills', 'Courses'] as const).map(name => <button key={name} aria-current={tab === name ? 'page' : undefined}
    onClick={() => { if (name !== tab) requestDirtyLeave(() => setTab(name)); }}>{name}</button>)}</nav>
    {tab === 'Skills' ? <SkillScreen onAuthLost={onAuthLost} /> : <CourseScreen onAuthLost={onAuthLost} />}</>;
}
