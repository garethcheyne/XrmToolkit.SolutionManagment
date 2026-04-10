import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { App } from './App';

// Initialize the bridge before React renders
import './bridge';

const root = document.getElementById('root');
if (root) {
  createRoot(root).render(
    <StrictMode>
      <App />
    </StrictMode>
  );
}
