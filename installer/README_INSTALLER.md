# RoboVAI POS Installer v5.0

This release path builds the final v5.0 desktop installer editions for customer delivery.

## Final Packaging Flow

1. Install Inno Setup 6 on the packaging machine: <https://jrsoftware.org/isinfo.php>
1. From the repository root, run:

```powershell
powershell -ExecutionPolicy Bypass -File .\installer\build-v5.ps1
```

1. The script will:

- publish the WPF app into `publish/final-exe`
- generate installer assets and EULA
- build two installer editions from `installer/SmartPOS.InnoSetup.v5.iss`
- write a release manifest with SHA256 hashes into `installer/Output`

## Output Files

- `installer/Output/RobovAI-PRO-POS-Setup-v5.0-Standard.exe`
- `installer/Output/RobovAI-PRO-POS-Setup-v5.0-Kaf5.exe`
- `installer/Output/FINAL_RELEASE_MANIFEST_v5.0.txt`

## Signing

To sign the published EXE and installers, either:

- place the PFX under `installer/cert/`, or
- pass `-CertThumbprint`, or
- set the `ROBOVAI_CERT_PASSWORD` environment variable if the PFX requires a password.

## What to Ship

- Ship to customers:
  - one of the final installer EXEs from `installer/Output`
  - the matching release manifest for archive and audit purposes

- Keep private (do NOT ship):
  - any private keys, PFX passwords, or internal signing assets
  - development tools and internal license generation utilities

## Notes

- The app DB and license state are stored under `%LocalAppData%\SmartPOS\` per Windows user.
- Change the default `admin` password immediately on first customer deployment.
- Validate backup, printer, and activation workflow on the target machine before handover.
