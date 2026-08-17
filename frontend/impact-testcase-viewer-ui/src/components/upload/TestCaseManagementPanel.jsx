import { useState } from 'react'
import ManualUploadPanel from './ManualUploadPanel'
import ReviewEditPanel from '../review/ReviewEditPanel'

export default function TestCaseManagementPanel({ user, loading, onSetLoading, onError, onCommitted }) {
  const [activeSubTab, setActiveSubTab] = useState('master')
  return (
    <section className="manual-upload-panel">
      <div className="view-tabs management-tabs">
        <button type="button" className={activeSubTab === 'master' ? 'active' : ''} onClick={() => setActiveSubTab('master')}>Master Test Case Upload</button>
        <button type="button" className={activeSubTab === 'results' ? 'active' : ''} onClick={() => setActiveSubTab('results')}>Test Result Upload</button>
        <button type="button" className={activeSubTab === 'review' ? 'active' : ''} onClick={() => setActiveSubTab('review')}>Review & Edit</button>
      </div>
      {activeSubTab === 'master' && <ManualUploadPanel user={user} loading={loading} onSetLoading={onSetLoading} onError={onError} onCommitted={onCommitted} uploadMode="master" />}
      {activeSubTab === 'results' && <ManualUploadPanel user={user} loading={loading} onSetLoading={onSetLoading} onError={onError} onCommitted={onCommitted} uploadMode="result" />}
      {activeSubTab === 'review' && <ReviewEditPanel user={user} loading={loading} onSetLoading={onSetLoading} onError={onError} />}
    </section>
  )
}
