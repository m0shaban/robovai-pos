"""
SmartPOS Offline License Key Generator
======================================
يولد activation keys متوافقة مع نظام LicenseService الموجود في SmartPOS

الـ algorithm:
- payload = JSON { "m": machine_id, "p": plan, "e": expiry_date, "g": generated_date }
- payload_b64 = base64url(utf8(json))
- secret = "mohamedshabanibrahimsalamaetmanrobovai:store.license-key.v1"
- signature = base64url(hmac_sha256(secret, payload_b64))
- token = payload_b64 + "." + signature

Usage:
  python generate_key.py                          <- generate trial key
  python generate_key.py --machine VQYAFH5I4W347PFQ --expiry 2027-04-27
  python generate_key.py --machine VQYAFH5I4W347PFQ --lifetime
"""

import argparse
import base64
import hashlib
import hmac
import json
import sys
from datetime import datetime, timedelta, timezone

# ─── CONFIG (match appsettings.json exactly) ─────────────────────────────────
OFFLINE_SECRET = "mohamedshabanibrahimsalamaetmanrobovai"
SECRET_SALT    = "store.license-key.v1"
DEFAULT_MACHINE = "VQYAFH5I4W347PFQ"   # ← device id shown in the app
# ─────────────────────────────────────────────────────────────────────────────


def base64url_encode(data: bytes) -> str:
    return base64.urlsafe_b64encode(data).rstrip(b"=").decode()


def base64url_decode(s: str) -> bytes:
    s = s.replace("-", "+").replace("_", "/")
    pad = 4 - len(s) % 4
    if pad != 4:
        s += "=" * pad
    return base64.b64decode(s)


def compute_signature(payload_b64: str) -> str:
    key = f"{OFFLINE_SECRET}:{SECRET_SALT}".encode("utf-8")
    msg = payload_b64.encode("utf-8")
    digest = hmac.new(key, msg, hashlib.sha256).digest()
    return base64url_encode(digest)


def normalize_machine_id(machine_id: str) -> str:
    return machine_id.strip().upper().replace(" ", "")


def generate_key(machine_id: str, expiry_date: str | None, plan: str = "Pro") -> str:
    machine_id = normalize_machine_id(machine_id)
    now_str = datetime.now(tz=timezone.utc).strftime("%Y-%m-%d")

    payload: dict = {
        "m": machine_id,
        "p": plan,
        "g": now_str,
    }

    if expiry_date is None or expiry_date.upper() == "LIFETIME":
        payload["e"] = "LIFETIME"
    else:
        payload["e"] = expiry_date  # format: "2027-04-27"

    payload_json = json.dumps(payload, ensure_ascii=False, separators=(",", ":"))
    payload_b64  = base64url_encode(payload_json.encode("utf-8"))
    signature    = compute_signature(payload_b64)

    token = f"{payload_b64}.{signature}"
    return token


def verify_key(token: str, machine_id: str | None = None):
    parts = token.strip().split(".")
    if len(parts) != 2:
        return False, "Invalid format (expected payload.signature)"

    payload_b64, sig = parts
    expected_sig = compute_signature(payload_b64)

    if not hmac.compare_digest(sig, expected_sig):
        return False, "Invalid signature ❌"

    payload_json = base64url_decode(payload_b64).decode("utf-8")
    payload      = json.loads(payload_json)

    if machine_id:
        m = normalize_machine_id(machine_id)
        if payload.get("m") != m:
            return False, f"Machine mismatch: key={payload.get('m')}, device={m}"

    expiry = payload.get("e", "")
    if expiry.upper() == "LIFETIME":
        return True, f"✅ LIFETIME key | Plan: {payload.get('p')} | Machine: {payload.get('m')}"
    else:
        try:
            exp_dt = datetime.strptime(expiry, "%Y-%m-%d").replace(tzinfo=timezone.utc)
            now    = datetime.now(tz=timezone.utc)
            if now <= exp_dt:
                days_left = (exp_dt - now).days
                return True, f"✅ Valid | Expires: {expiry} | Days left: {days_left} | Plan: {payload.get('p')}"
            else:
                return False, f"⚠️ Expired on {expiry}"
        except Exception as ex:
            return False, f"Date parse error: {ex}"


def main():
    parser = argparse.ArgumentParser(description="SmartPOS License Key Generator")
    parser.add_argument("--machine",  default=DEFAULT_MACHINE, help="Machine/Device ID from the app")
    parser.add_argument("--expiry",   default=None, help="Expiry date YYYY-MM-DD (or LIFETIME)")
    parser.add_argument("--lifetime", action="store_true", help="Generate a lifetime key")
    parser.add_argument("--plan",     default="Pro", help="Plan name (Pro, Basic, Trial)")
    parser.add_argument("--days",     type=int, default=365, help="Days from today if no --expiry")
    parser.add_argument("--verify",   default=None, help="Verify an existing key token")

    args = parser.parse_args()

    if args.verify:
        ok, msg = verify_key(args.verify, args.machine)
        print(f"\nVerification: {msg}")
        sys.exit(0 if ok else 1)

    # Determine expiry
    if args.lifetime:
        expiry = "LIFETIME"
    elif args.expiry:
        expiry = args.expiry
    else:
        expiry = (datetime.now(tz=timezone.utc) + timedelta(days=args.days)).strftime("%Y-%m-%d")

    machine = normalize_machine_id(args.machine)
    token   = generate_key(machine, expiry if expiry != "LIFETIME" else None, plan=args.plan)

    print("\n" + "="*60)
    print("  SmartPOS License Key Generator")
    print("="*60)
    print(f"  Machine ID : {machine}")
    print(f"  Plan       : {args.plan}")
    print(f"  Expiry     : {expiry}")
    print(f"  Generated  : {datetime.now().strftime('%Y-%m-%d %H:%M')}")
    print("="*60)
    print(f"\n  LICENSE KEY:\n")
    print(f"  {token}")
    print("\n" + "="*60)

    # Auto-verify
    ok, msg = verify_key(token, machine)
    print(f"  Self-check : {msg}")
    print("="*60 + "\n")

    # Copy to clipboard on Windows
    try:
        import subprocess
        subprocess.run(["clip"], input=token.encode("utf-8"), check=True)
        print("  ✅ Key copied to clipboard!")
    except Exception:
        pass


if __name__ == "__main__":
    # Quick test if no args
    if len(sys.argv) == 1:
        print("\n[Quick Demo] Generating key for default machine...\n")
        # 1-year key
        token = generate_key(DEFAULT_MACHINE, None, plan="Pro")
        print(f"1-Year Key:\n{token}\n")

        # Lifetime key
        token_lt = generate_key(DEFAULT_MACHINE, None, plan="Pro")
        token_lt_lt = generate_key(DEFAULT_MACHINE, "LIFETIME", plan="Pro")
        print(f"LIFETIME Key:\n{token_lt_lt}\n")

        ok, msg = verify_key(token, DEFAULT_MACHINE)
        print(f"Verify 1-Year: {msg}")

        ok2, msg2 = verify_key(token_lt_lt, DEFAULT_MACHINE)
        print(f"Verify LIFETIME: {msg2}")

    main()
