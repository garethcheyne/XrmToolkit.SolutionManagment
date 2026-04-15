import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { App } from './App';
import { HelpApp } from './HelpApp';
import { ErrorBoundary } from './ErrorBoundary';

// Initialize the bridge before React renders
import './bridge';

const isHelpOnly = !!(window as unknown as { __helpOnly?: boolean }).__helpOnly;

const root = document.getElementById('root');
if (root) {
  createRoot(root).render(
    <StrictMode>
      <ErrorBoundary>
        {isHelpOnly ? <HelpApp /> : <App />}
      </ErrorBoundary>
    </StrictMode>
  );
}
