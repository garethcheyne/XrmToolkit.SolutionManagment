import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { DocsViewer } from './tabs/DocsViewer';

/**
 * Minimal app shell for the pop-out help window.
 * Shows only the documentation viewer — no tabs, connection bar, or other UI.
 */
export function HelpApp() {
    return (
        <FluentProvider theme={webLightTheme} style={{ height: '100vh', overflow: 'hidden' }}>
            <DocsViewer />
        </FluentProvider>
    );
}
