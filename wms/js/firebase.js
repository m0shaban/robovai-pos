/**
 * @license
 * SPDX-License-Identifier: Apache-2.0
 */

/**
 * Firebase Service — Cloud Auth + Firestore Sync
 *
 * Designed as an optional enhancement layer. When VITE_FIREBASE_API_KEY is not
 * configured the _ok flag stays false and every export returns a no-op so the
 * rest of the app continues to work fully offline.
 */

import { deleteApp, initializeApp } from 'firebase/app';
import {
  browserLocalPersistence,
  createUserWithEmailAndPassword,
  signOut as fbSignOut,
  getAuth,
  setPersistence,
  signInWithEmailAndPassword,
} from 'firebase/auth';
import {
  collection,
  doc,
  getDocs,
  getFirestore,
  query,
  serverTimestamp,
  setDoc,
  updateDoc,
  where,
  writeBatch,
} from 'firebase/firestore';

// ---------------------------------------------------------------------------
// Initialisation
// ---------------------------------------------------------------------------

const _cfg = {
  apiKey: import.meta.env.VITE_FIREBASE_API_KEY,
  authDomain: import.meta.env.VITE_FIREBASE_AUTH_DOMAIN,
  projectId: import.meta.env.VITE_FIREBASE_PROJECT_ID,
  storageBucket: import.meta.env.VITE_FIREBASE_STORAGE_BUCKET,
  messagingSenderId: import.meta.env.VITE_FIREBASE_MESSAGING_SENDER_ID,
  appId: import.meta.env.VITE_FIREBASE_APP_ID,
};

let _app = null;
let _auth = null;
let _db = null;
let _ok = false;

try {
  const hasKey =
    _cfg.apiKey &&
    _cfg.apiKey.length > 10 &&
    _cfg.apiKey !== 'your_api_key_here';
  const hasProject = _cfg.projectId && _cfg.projectId !== 'your_project_id';
  if (hasKey && hasProject) {
    _app = initializeApp(_cfg);
    _auth = getAuth(_app);
    _db = getFirestore(_app);
    setPersistence(_auth, browserLocalPersistence).catch(() => {});
    _ok = true;
    console.log('[Firebase] Initialized successfully');
  } else {
    console.log(
      '[Firebase] Config not provided — running in offline-only mode',
    );
  }
} catch (e) {
  console.warn('[Firebase] Init failed:', e.message);
}

// ---------------------------------------------------------------------------
// Account ID helpers (stored in localStorage)
// ---------------------------------------------------------------------------

export const getAccountId = () => localStorage.getItem('robovai_account_id');
export const setAccountId = (uid) =>
  localStorage.setItem('robovai_account_id', uid);

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/**
 * Synthesise a deterministic email for sub-users so they can authenticate
 * with Firebase Auth without the owner needing the Admin SDK.
 */
function buildEmail(username, accountId) {
  // Sanitise username: only lowercase alphanumeric + dash
  const safeName = username.toLowerCase().replace(/[^a-z0-9-]/g, '-');
  const shortId = accountId.replace(/-/g, '').slice(0, 20);
  return `${safeName}@${shortId}.wms.app`;
}

/**
 * Translate Firebase error codes to user-friendly Arabic messages.
 */
function translateFirebaseError(error) {
  const code = error?.code || '';
  const msgs = {
    'auth/email-already-in-use':
      'هذا البريد الإلكتروني مسجّل مسبقاً. إذا نسيت البيانات اضغط "استرداد الحساب"، أو استخدم بريداً آخر.',
    'auth/invalid-email': 'البريد الإلكتروني غير صالح — تحقق من الصيغة.',
    'auth/weak-password':
      'كلمة المرور ضعيفة جداً — يجب أن تكون 6 أحرف على الأقل.',
    'auth/operation-not-allowed':
      'تسجيل الدخول بالبريد الإلكتروني غير مُفعَّل في هذا المشروع. تواصل مع المطوّر لتفعيله.',
    'auth/network-request-failed':
      'خطأ في الشبكة — تحقق من الاتصال بالإنترنت وحاول مجدداً.',
    'auth/too-many-requests':
      'محاولات كثيرة متتالية — انتظر بضع دقائق ثم حاول مجدداً.',
    'auth/user-not-found': 'المستخدم غير موجود.',
    'auth/wrong-password': 'كلمة المرور خاطئة.',
    'auth/invalid-credential': 'بيانات الدخول غير صحيحة.',
    'auth/user-disabled': 'هذا الحساب موقوف. تواصل مع المدير.',
    'auth/requires-recent-login': 'يرجى تسجيل الدخول مجدداً ثم إعادة المحاولة.',
    'permission-denied':
      'رُفض الوصول إلى قاعدة البيانات — تحقق من إعدادات Firestore Security Rules.',
  };
  return msgs[code] || error?.message || 'حدث خطأ غير متوقع في Firebase.';
}

// ---------------------------------------------------------------------------
// Account Registration (first-time setup — called by the owner)
// ---------------------------------------------------------------------------

/**
 * Register a new WMS account. Creates a Firebase Auth user for the owner,
 * then writes the account document and the owner's user profile to Firestore.
 *
 * @param {string} storeName  Display name for the store
 * @param {string} email      Owner's real email address
 * @param {string} password   Password (min 6 chars required by Firebase Auth)
 * @returns {{ uid: string, storeName: string, email: string }}
 */
export async function fbRegisterAccount(storeName, email, password) {
  if (!_ok)
    throw new Error('Firebase غير مُهيأ — يعمل النظام في وضع أوفلاين فقط.');
  try {
    const cred = await createUserWithEmailAndPassword(_auth, email, password);
    const uid = cred.user.uid;

    await setDoc(doc(_db, 'accounts', uid), {
      storeName,
      ownerEmail: email,
      ownerUid: uid,
      createdAt: serverTimestamp(),
    });

    // Owner user profile inside the account's sub-collection
    await setDoc(doc(_db, 'accounts', uid, 'users', uid), {
      username: 'admin',
      email,
      role: 'super_admin',
      active: true,
      createdAt: serverTimestamp(),
    });

    // Only mark as registered AFTER both Firestore writes succeed
    setAccountId(uid);

    return { uid, storeName, email };
  } catch (e) {
    throw new Error(translateFirebaseError(e));
  }
}

// ---------------------------------------------------------------------------
// Account Recovery (write missing Firestore docs for existing Auth user)
// ---------------------------------------------------------------------------

/**
 * Recover Firestore account documents when Firebase Auth user already exists
 * but Firestore docs were never written (e.g., due to old security rules).
 * Call this when the user is already authenticated via Firebase Auth.
 *
 * @param {string} storeName  Display name for the store
 * @param {string} email      Owner's email (must match the signed-in user)
 * @returns {{ uid: string, storeName: string, email: string }}
 */
export async function fbRecoverAccountDocs(storeName, email) {
  if (!_ok) throw new Error('Firebase not configured');
  const user = _auth.currentUser;
  if (!user)
    throw new Error(
      'No authenticated Firebase user. Please reload and try again.',
    );

  const uid = user.uid;
  setAccountId(uid);

  await setDoc(
    doc(_db, 'accounts', uid),
    {
      storeName,
      ownerEmail: email,
      ownerUid: uid,
      createdAt: serverTimestamp(),
    },
    { merge: true },
  );

  await setDoc(
    doc(_db, 'accounts', uid, 'users', uid),
    {
      username: 'admin',
      email,
      role: 'super_admin',
      active: true,
      createdAt: serverTimestamp(),
    },
    { merge: true },
  );

  return { uid, storeName, email };
}

// ---------------------------------------------------------------------------
// Login
// ---------------------------------------------------------------------------

/**
 * Log in by username. Looks up the email from Firestore, then signs in with
 * Firebase Auth. Returns a user-info object compatible with the existing
 * AuthService shape.
 */
export async function fbLoginByUsername(username, password) {
  const accountId = getAccountId();
  if (!accountId)
    throw new Error('الحساب غير مُعد بعد. يرجى إعداد الحساب أولاً');

  const usersRef = collection(_db, 'accounts', accountId, 'users');
  const snap = await getDocs(
    query(usersRef, where('username', '==', username)),
  );

  if (snap.empty) throw new Error('اسم المستخدم غير موجود في الحساب');

  const userDoc = snap.docs[0];
  const userData = userDoc.data();

  if (userData.active === false)
    throw new Error('هذا الحساب موقوف. تواصل مع المدير');

  try {
    await signInWithEmailAndPassword(_auth, userData.email, password);
  } catch (e) {
    throw new Error(translateFirebaseError(e));
  }

  return {
    uid: userDoc.id,
    username: userData.username,
    role: userData.role,
    email: userData.email,
    accountId,
    cloud: true,
  };
}

// ---------------------------------------------------------------------------
// Sub-user Management
// ---------------------------------------------------------------------------

/**
 * Create a sub-user under the current account using a temporary secondary
 * Firebase App (avoids displacing the currently-signed-in owner).
 */
export async function fbCreateSubUser(username, password, role) {
  const accountId = getAccountId();
  if (!accountId) throw new Error('الحساب غير مُعد');

  const email = buildEmail(username, accountId);
  const appName = `create-user-${Date.now()}`;
  const tempApp = initializeApp(_cfg, appName);
  const tempAuth = getAuth(tempApp);

  try {
    const cred = await createUserWithEmailAndPassword(
      tempAuth,
      email,
      password,
    );
    const uid = cred.user.uid;

    await setDoc(doc(_db, 'accounts', accountId, 'users', uid), {
      username,
      email,
      role,
      active: true,
      createdAt: serverTimestamp(),
    });

    return uid;
  } finally {
    // Always clean up the temporary app to avoid memory leaks
    await fbSignOut(tempAuth).catch(() => {});
    await deleteApp(tempApp).catch(() => {});
  }
}

/**
 * Soft-delete a sub-user by marking them inactive in Firestore.
 * (Hard-deletion of Firebase Auth users requires the Admin SDK.)
 */
export async function fbDeleteSubUser(uid) {
  const accountId = getAccountId();
  if (!accountId) return;
  await updateDoc(doc(_db, 'accounts', accountId, 'users', uid), {
    active: false,
  });
}

/**
 * List all active users in the current account.
 */
export async function fbListUsers() {
  const accountId = getAccountId();
  if (!accountId) return [];
  const snap = await getDocs(collection(_db, 'accounts', accountId, 'users'));
  return snap.docs
    .map((d) => ({ uid: d.id, ...d.data() }))
    .filter((u) => u.active !== false);
}

// ---------------------------------------------------------------------------
// Sign Out
// ---------------------------------------------------------------------------

export async function fbSignOutUser() {
  if (_ok && _auth) await fbSignOut(_auth).catch(() => {});
}

// ---------------------------------------------------------------------------
// Firestore Sync — Products
// ---------------------------------------------------------------------------

/**
 * Push locally-pending products to Firestore using a write batch.
 * Returns the number of records synced.
 */
export async function fbSyncProducts(dexieDb) {
  if (!_ok) return 0;
  const accountId = getAccountId();
  if (!accountId) return 0;

  const pending = await dexieDb.products
    .where('sync_status')
    .equals('pending')
    .toArray();
  if (!pending.length) return 0;

  const batch = writeBatch(_db);
  for (const p of pending) {
    const { id, ...data } = p;
    const cloudId = p.robovai_sync_id || String(id);
    batch.set(
      doc(_db, 'accounts', accountId, 'products', cloudId),
      { ...data, dexie_id: id, lastSynced: serverTimestamp() },
      { merge: true },
    );
  }
  await batch.commit();

  // Mark as synced locally
  await dexieDb.products
    .where('id')
    .anyOf(pending.map((p) => p.id))
    .modify({ sync_status: 'synced' });

  return pending.length;
}

// ---------------------------------------------------------------------------
// Firestore Sync — Transactions
// ---------------------------------------------------------------------------

/**
 * Push locally-pending transactions to Firestore.
 * Returns the number of records synced.
 */
export async function fbSyncTransactions(dexieDb) {
  if (!_ok) return 0;
  const accountId = getAccountId();
  if (!accountId) return 0;

  const pending = await dexieDb.transactions
    .where('sync_status')
    .equals('pending')
    .toArray();
  if (!pending.length) return 0;

  const batch = writeBatch(_db);
  for (const t of pending) {
    const { id, ...data } = t;
    const cloudId = t.robovai_sync_id || String(id);
    batch.set(
      doc(_db, 'accounts', accountId, 'transactions', cloudId),
      { ...data, dexie_id: id, lastSynced: serverTimestamp() },
      { merge: true },
    );
  }
  await batch.commit();

  await dexieDb.transactions
    .where('id')
    .anyOf(pending.map((t) => t.id))
    .modify({ sync_status: 'synced' });

  return pending.length;
}

// ---------------------------------------------------------------------------
// Firestore Pull — Products (cross-device restore)
// ---------------------------------------------------------------------------

/**
 * Pull all cloud products that don't exist locally and add them to Dexie.
 * Returns the number of records pulled.
 */
export async function fbPullProducts(dexieDb) {
  if (!_ok) return 0;
  const accountId = getAccountId();
  if (!accountId) return 0;

  const snap = await getDocs(
    collection(_db, 'accounts', accountId, 'products'),
  );
  const cloudProducts = snap.docs.map((d) => ({
    ...d.data(),
    robovai_sync_id: d.id,
  }));

  let pulled = 0;
  for (const cp of cloudProducts) {
    const existing = await dexieDb.products
      .where('robovai_sync_id')
      .equals(cp.robovai_sync_id)
      .first();

    if (!existing) {
      const { dexie_id, lastSynced, ...productData } = cp;
      await dexieDb.products.add({ ...productData, sync_status: 'synced' });
      pulled++;
    }
  }

  return pulled;
}

// ---------------------------------------------------------------------------
// Default export — FirebaseService object
// ---------------------------------------------------------------------------

const FirebaseService = {
  get isConfigured() {
    return _ok;
  },
  get isAccountSetup() {
    return !!getAccountId();
  },
  getAccountId,
  setAccountId,
  registerAccount: fbRegisterAccount,
  recoverAccountDocs: fbRecoverAccountDocs,
  loginByUsername: fbLoginByUsername,
  createSubUser: fbCreateSubUser,
  deleteSubUser: fbDeleteSubUser,
  listUsers: fbListUsers,
  signOut: fbSignOutUser,
  syncProducts: fbSyncProducts,
  syncTransactions: fbSyncTransactions,
  pullProducts: fbPullProducts,
};

export default FirebaseService;
export { _ok as fbOk };
