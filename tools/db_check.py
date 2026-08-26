import sqlite3, os, json

db = os.path.expandvars(r'%LOCALAPPDATA%\SmartPOS\smartpos.db')
conn = sqlite3.connect(db)
c = conn.cursor()

print('=== DATABASE HEALTH CHECK ===')
print()

tables = c.execute("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name").fetchall()
print('Tables:', [t[0] for t in tables])
print()

for table in ['Users', 'Products', 'Categories', 'Customers', 'Suppliers', 'Sales', 'SaleDetails', 'Expenses', 'Shifts', 'Tables', 'PurchaseOrders', 'Returns', 'CustomerLoyalties', 'AppSettings']:
    try:
        count = c.execute(f'SELECT COUNT(*) FROM {table}').fetchone()[0]
        print(f'  {table}: {count} rows')
    except Exception as ex:
        print(f'  {table}: TABLE MISSING! ({ex})')

print()
print('=== USER DETAILS ===')
c.execute('SELECT Id, Username, PasswordHash, FullName, Role, IsActive FROM Users')
for row in c.fetchall():
    print(f'  [{row[0]}] {row[1]} / {row[2]} | {row[3]} | Role={row[4]} | Active={row[5]}')

print()
print('=== LICENSE STATUS ===')
license_path = os.path.expandvars(r'%LOCALAPPDATA%\SmartPOS\license.json')
if os.path.exists(license_path):
    with open(license_path) as f:
        raw = f.read().strip()
        if not raw:
            print('License file exists but is empty')
        else:
            try:
                data = json.loads(raw)
                print(json.dumps(data, indent=2, default=str)[:500])
            except json.JSONDecodeError as ex:
                print(f'License file is invalid JSON: {ex}')
else:
    print('No license file')

conn.close()
