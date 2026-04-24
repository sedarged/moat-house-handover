const pendingRequests = new Map();

function getBridgeApi() {
  return window.chrome?.webview;
}

export function initializeHostBridge() {
  const bridge = getBridgeApi();
  if (!bridge) {
    return;
  }

  bridge.addEventListener('message', (event) => {
    const message = event.data;
    if (!message?.requestId || !pendingRequests.has(message.requestId)) {
      return;
    }

    const pending = pendingRequests.get(message.requestId);
    pendingRequests.delete(message.requestId);

    if (!message.success) {
      pending.reject(new Error(message.error || 'Unknown host bridge error.'));
      return;
    }

    pending.resolve(message.payload);
  });
}

export async function hostRequest(type, payload = null) {
  const bridge = getBridgeApi();
  if (!bridge) {
    throw new Error('Host bridge unavailable. Running in browser/dev mode.');
  }

  const requestId = crypto.randomUUID();
  const request = { requestId, type, payload };

  const result = new Promise((resolve, reject) => {
    pendingRequests.set(requestId, { resolve, reject });
  });

  bridge.postMessage(request);
  return result;
}

export async function getRuntimeStatus() {
  return hostRequest('runtime.getStatus');
}
