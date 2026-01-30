import { getBasePath } from './config';

function getApiBase(): string {
  return `${getBasePath()}/api/v1`;
}

function getAuthHeader(): string {
  const credentials = localStorage.getItem('mkat_credentials');
  if (!credentials) return '';
  return `Basic ${credentials}`;
}

export async function initNotifications(): Promise<void> {
  if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
    console.warn('Push notifications not supported');
    return;
  }

  try {
    const registration = await navigator.serviceWorker.register('./sw.js');
    console.log('Service worker registered');

    const permission = await Notification.requestPermission();
    if (permission !== 'granted') {
      console.warn('Notification permission denied');
      return;
    }

    await subscribeToPush(registration);
  } catch (err) {
    console.error('Failed to init notifications:', err);
  }
}

async function subscribeToPush(registration: ServiceWorkerRegistration): Promise<void> {
  const authHeader = getAuthHeader();
  if (!authHeader) return;

  const keyResponse = await fetch(`${getApiBase()}/push/vapid-public-key`, {
    headers: { Authorization: authHeader },
  });
  if (!keyResponse.ok) return;

  const { publicKey } = await keyResponse.json();
  if (!publicKey) return;

  let subscription = await registration.pushManager.getSubscription();

  if (!subscription) {
    subscription = await registration.pushManager.subscribe({
      userVisibleOnly: true,
      applicationServerKey: urlBase64ToUint8Array(publicKey).buffer.slice(0) as ArrayBuffer,
    });
  }

  const sub = subscription.toJSON();
  await fetch(`${getApiBase()}/push/subscribe`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Authorization: authHeader,
    },
    body: JSON.stringify({
      endpoint: sub.endpoint,
      keys: {
        p256dh: sub.keys?.p256dh || '',
        auth: sub.keys?.auth || '',
      },
    }),
  });
}

function urlBase64ToUint8Array(base64String: string): Uint8Array {
  const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
  const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
  const rawData = window.atob(base64);
  const outputArray = new Uint8Array(rawData.length);
  for (let i = 0; i < rawData.length; ++i) {
    outputArray[i] = rawData.charCodeAt(i);
  }
  return outputArray;
}
