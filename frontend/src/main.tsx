import React from 'react';
import ReactDOM from 'react-dom/client';
import './styles/variables.css';
import './styles/global.css';
import App from './App';

async function initAccessibilityAudit() {
  if (import.meta.env.DEV) {
    const { default: axe } = await import('@axe-core/react');
    axe(React, ReactDOM, 1000);
  }
}

initAccessibilityAudit();

ReactDOM.createRoot(document.getElementById('root') as HTMLElement).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);
