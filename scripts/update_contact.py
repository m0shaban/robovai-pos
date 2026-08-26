import os

def replace_in_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    content = content.replace('201018501659', '201121891913')
    content = content.replace('+20 101 850 1659', '+20 112 189 1913')
    content = content.replace('Info@robovai.tech', 'robovaisolutions@gmail.com')
    content = content.replace('مقر الشركة: القاهرة، مصر', 'مقر الشركة: مدينه 6 اكتوبر الحي الثاني مصر')

    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)

replace_in_file('LandingPage/index.html')
replace_in_file('LandingPage/manual.html')

# Add PDF print button to manual.html
with open('LandingPage/manual.html', 'r', encoding='utf-8') as f:
    manual_content = f.read()

if 'طباعة الدليل PDF' not in manual_content:
    button_html = '''
    <!-- Print to PDF Button -->
    <button onclick="window.print()" class="btn btn-blue print-btn" style="position: fixed; bottom: 30px; left: 30px; z-index: 9999; box-shadow: 0 8px 32px rgba(14,165,233,0.4); border-radius: 50px; padding: 14px 28px; font-size: 1.1rem; display: flex; align-items: center; gap: 10px; cursor: pointer;">
        <i class="fas fa-file-pdf"></i> طباعة الدليل PDF
    </button>
    <style>
        @media print {
            .print-btn, .navbar, .footer { display: none !important; }
            body { background: white !important; color: black !important; }
            .manual-container { padding: 0 !important; margin: 0 !important; box-shadow: none !important; }
        }
    </style>
</body>
'''
    manual_content = manual_content.replace('</body>', button_html)
    with open('LandingPage/manual.html', 'w', encoding='utf-8') as f:
        f.write(manual_content)
print('Updates applied successfully!')
