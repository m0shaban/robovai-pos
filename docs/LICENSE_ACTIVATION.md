# Unified Activation (RoboVAI Compatible)

This build uses the same activation format as robovai.tech.

- Key format: `payload_b64.signature_b64`
- Signature: `HMAC-SHA256`
- Signing secret format: `LICENSE_SECRET_KEY:store.license-key.v1`
- Machine binding: payload field `m`
- Expiry field: payload field `e` (`YYYY-MM-DD` or `LIFETIME`)
- Grace period: 3 days after expiry

The same code can be verified in two modes:

1. Offline mode (local signature verification in app).
2. Online mode (server verification through `POST /api/licenses/verify/`).

## Customer Flow

1. Run the app.
2. If activation is required, the activation window shows a **Device ID**.
3. Customer sends the Device ID.
4. You issue a RoboVAI-compatible activation key and send it back.
5. Customer pastes the key in app activation window.

## Activation Settings

Configure `src/SmartPOS.WPF/appsettings.json`:

```json
"LicenseActivation": {
  "OfflineSecretKey": "CHANGE_ME_TO_ROBOVAI_LICENSE_SECRET",
  "SecretSalt": "store.license-key.v1",
  "VerifyEndpoint": "https://robovai.tech/api/licenses/verify/",
  "EnableOnlineVerification": true,
  "OnlineTimeoutSeconds": 20
}
```

Important:

- Use the same `LICENSE_SECRET_KEY` from robovai.tech inside `OfflineSecretKey`.
- Keep the secret private and do not expose it outside trusted environments.

## Offline Backup HTML Tool

A complete standalone backup generator/verifier is included:

- `activation_backup_offline.html`

You can open it locally without internet and use it to:

1. Generate keys in RoboVAI format.
2. Verify keys for a machine ID.

The file runs fully offline in modern browsers.

## Where the license is stored

- Per user:
  - `%LocalAppData%\SmartPOS\license.json`

## Grace period behavior

- Valid: app runs normally.
- Expired but within grace (3 days): app runs and shows a warning.
- Expired beyond grace: app requires activation before continuing.
