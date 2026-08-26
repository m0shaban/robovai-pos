import sqlite3
import sys

conn = sqlite3.connect('smartpos.db')
c = conn.cursor()

c.execute('DELETE FROM Products')
c.execute('DELETE FROM Categories')

c.execute("INSERT INTO Categories (Id, Name, ColorCode, IsActive, IsDeleted, CreatedAt, UpdatedAt) VALUES (1, 'Beverages', '#FF0000', 1, 0, '2026-04-01', '2026-04-01')")
c.execute("INSERT INTO Categories (Id, Name, ColorCode, IsActive, IsDeleted, CreatedAt, UpdatedAt) VALUES (2, 'Snacks', '#00FF00', 1, 0, '2026-04-01', '2026-04-01')")
c.execute("INSERT INTO Categories (Id, Name, ColorCode, IsActive, IsDeleted, CreatedAt, UpdatedAt) VALUES (3, 'Hot Drinks', '#0000FF', 1, 0, '2026-04-01', '2026-04-01')")

now = '2026-04-01 00:00:00'
products = [
    ('Coca Cola',        '1001', 15.0, 20.0, 100, 1, 1, 0, now, 10, 1),
    ('Pepsi Can',        '1002',  8.0, 12.0,  80, 1, 1, 0, now, 10, 1),
    ('Beti Juice',       '1003', 10.0, 15.0, 150, 1, 1, 0, now, 10, 1),
    ('Small Water',      '1004',  3.0,  5.0, 300, 1, 1, 0, now,  5, 1),
    ('Chipsy Family',    '1005', 18.0, 25.0,  50, 2, 1, 0, now,  5, 1),
    ('Galaxy Chocolate', '1006', 28.0, 35.0, 200, 2, 1, 0, now, 10, 1),
    ('Lays Original',    '1007', 15.0, 20.0,  75, 2, 1, 0, now,  5, 1),
    ('Nescafe Black',    '1008',  2.0,  3.5, 500, 3, 1, 0, now, 20, 1),
    ('Cappuccino',       '1009',  8.0, 15.0, 100, 3, 1, 0, now, 10, 1),
]

c.executemany('''INSERT INTO Products 
    (Name, Barcode, PurchasePrice, SellingPrice, Stock, CategoryId, IsActive, IsDeleted, CreatedAt, MinStockLevel, Unit) 
    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)''', products)

conn.commit()
sys.stdout.buffer.write(b"Done! 3 Categories + 9 Products inserted.\n")
conn.close()
