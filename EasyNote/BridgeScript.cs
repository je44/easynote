namespace EasyNote;

internal static class BridgeScript
{
    public const string Init = """
        window.__EASYNOTE_BRIDGE__ = true;

        window.__invoke = function(cmd, args) {
            return new Promise((resolve, reject) => {
                const id = Math.random().toString(36).slice(2);
                window.__pendingCalls = window.__pendingCalls || {};
                window.__pendingCalls[id] = { resolve, reject };
                window.chrome.webview.postMessage(JSON.stringify({ id, cmd, args: args || {} }));
            });
        };

        window.chrome.webview.addEventListener('message', function(e) {
            const msg = JSON.parse(e.data);
            if (msg.type === 'response') {
                const pending = (window.__pendingCalls || {})[msg.id];
                if (pending) {
                    delete window.__pendingCalls[msg.id];
                    if (msg.ok) pending.resolve(msg.result);
                    else pending.reject(new Error(msg.error));
                }
            }
        });

        window.__startDrag = function() {
            window.__invoke('start_drag', {});
        };

        document.addEventListener('pointerdown', function() {
            window.__invoke('ensure_interactive', {});
        }, true);
        """;
}
