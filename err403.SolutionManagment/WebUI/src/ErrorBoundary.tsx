import { Component, type ErrorInfo, type ReactNode } from 'react';

interface Props {
  children: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
}

export class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false, error: null, errorInfo: null };
  }

  static getDerivedStateFromError(error: Error): Partial<State> {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    this.setState({ errorInfo });

    // Log to trace via bridge if available
    const msg = `[ErrorBoundary] ${error.name}: ${error.message}\n${errorInfo.componentStack}`;
    console.error(msg);
    try {
      const w = window as unknown as { chrome?: { webview?: { postMessage?: (m: string) => void } } };
      w.chrome?.webview?.postMessage?.(JSON.stringify({
        action: 'logError',
        message: msg,
      }));
    } catch {
      // ignore bridge errors
    }
  }

  render() {
    if (this.state.hasError) {
      return (
        <div style={{
          padding: '32px',
          fontFamily: 'Segoe UI, sans-serif',
          color: '#242424',
          backgroundColor: '#fafafa',
          height: '100vh',
          boxSizing: 'border-box',
          overflow: 'auto',
        }}>
          <h2 style={{ color: '#c4314b', marginTop: 0, fontSize: '20px' }}>
            Something went wrong
          </h2>
          <p style={{ color: '#424242', fontSize: '14px', margin: '0 0 16px' }}>
            The plugin encountered an unexpected error. Click "Try again" to recover.
          </p>
          <button
            onClick={() => this.setState({ hasError: false, error: null, errorInfo: null })}
            style={{
              padding: '6px 16px',
              fontSize: '14px',
              cursor: 'pointer',
              border: '1px solid #d1d1d1',
              borderRadius: '4px',
              backgroundColor: '#ffffff',
              color: '#242424',
              marginBottom: '16px',
            }}
          >
            Try again
          </button>
          <details style={{ marginTop: '8px' }}>
            <summary style={{ cursor: 'pointer', fontWeight: 600, fontSize: '13px', color: '#424242' }}>
              Error details
            </summary>
            <div style={{
              marginTop: '8px',
              padding: '12px',
              backgroundColor: '#f0f0f0',
              border: '1px solid #e0e0e0',
              borderRadius: '4px',
              fontSize: '12px',
              fontFamily: 'Consolas, Courier New, monospace',
              whiteSpace: 'pre-wrap',
              wordBreak: 'break-word',
              maxHeight: '300px',
              overflow: 'auto',
              color: '#1a1a1a',
              lineHeight: '1.5',
              userSelect: 'text',
            }}>
              {this.state.error?.toString()}
              {'\n\nComponent Stack:'}
              {this.state.errorInfo?.componentStack}
            </div>
          </details>
        </div>
      );
    }

    return this.props.children;
  }
}
