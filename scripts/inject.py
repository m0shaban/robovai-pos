import sys

with open('LandingPage/index.html', 'r', encoding='utf-8') as f:
    content = f.read()

pos_demo_html = """
    <!-- Interactive POS Demo -->
    <section class="section">
        <div class="container reveal">
            <h2 class="section-title">محاكي الكاشير السريع ⚡</h2>
            <p class="section-subtitle">جرب تضرب أوردر بنفسك وشوف السرعة اللي هتخلي الزحمة تختفي في ثواني.</p>
            <div class="pos-demo-container ios-glass">
                <div class="scan-flash" id="demo-flash"></div>
                <div class="pos-demo-items">
                    <div class="pos-item-btn" onclick="addToReceipt('قهوة اسبريسو', 35)">
                        <i class="fas fa-coffee"></i>
                        <div class="pos-item-name">قهوة اسبريسو</div>
                        <div class="pos-item-price">35 ج.م</div>
                    </div>
                    <div class="pos-item-btn" onclick="addToReceipt('برجر لحم', 120)">
                        <i class="fas fa-hamburger"></i>
                        <div class="pos-item-name">برجر لحم</div>
                        <div class="pos-item-price">120 ج.م</div>
                    </div>
                    <div class="pos-item-btn" onclick="addToReceipt('بيتزا مارجريتا', 150)">
                        <i class="fas fa-pizza-slice"></i>
                        <div class="pos-item-name">بيتزا مارجريتا</div>
                        <div class="pos-item-price">150 ج.م</div>
                    </div>
                    <div class="pos-item-btn" onclick="addToReceipt('كولا كانز', 20)">
                        <i class="fas fa-wine-glass-alt"></i>
                        <div class="pos-item-name">كولا كانز</div>
                        <div class="pos-item-price">20 ج.م</div>
                    </div>
                    <div class="pos-item-btn" onclick="addToReceipt('آيس كريم', 40)">
                        <i class="fas fa-ice-cream"></i>
                        <div class="pos-item-name">آيس كريم</div>
                        <div class="pos-item-price">40 ج.م</div>
                    </div>
                    <div class="pos-item-btn" style="background: rgba(231, 76, 60, 0.2); border-color: #e74c3c;" onclick="clearReceipt()">
                        <i class="fas fa-trash" style="color: #e74c3c;"></i>
                        <div class="pos-item-name">إلغاء الفاتورة</div>
                        <div class="pos-item-price">Clear</div>
                    </div>
                </div>
                <div class="pos-demo-receipt">
                    <div class="receipt-header">فاتورة مبيعات - رقم 1042</div>
                    <div class="receipt-items" id="receipt-items-container">
                        <div id="empty-receipt-msg" style="text-align: center; margin-top: 50px; color: #999;">اضغط على أي صنف لإضافته للفاتورة</div>
                    </div>
                    <div class="receipt-total">
                        <span>الإجمالي:</span>
                        <span id="receipt-total-value">0 ج.م</span>
                    </div>
                </div>
            </div>
        </div>
    </section>
"""

roi_calc_html = """
    <!-- ROI Calculator -->
    <section class="section">
        <div class="container reveal">
            <h2 class="section-title">احسب أرباحك الضايعة 💸</h2>
            <p class="section-subtitle">حرك الشريط واعرف RoboVAI هيوفرلك كام من إغلاق ثغرات الكاشير (المتوسط 3% لـ 5% هدر وسرقات).</p>
            <div class="roi-calculator ios-glass">
                <h3>متوسط مبيعاتك اليومية: <span id="daily-sales-label" style="color: var(--primary);">5,000</span> ج.م</h3>
                <div class="roi-slider-container">
                    <input type="range" min="1000" max="50000" step="500" value="5000" class="range-slider" id="roiSlider" oninput="calculateROI()">
                    <div style="display: flex; justify-content: space-between; width: 100%; margin-top: 10px; color: #888;">
                        <span>1,000</span>
                        <span>50,000</span>
                    </div>
                </div>
                <div class="roi-result">
                    <h4>السيستم هيوفرلك شهرياً حوالي:</h4>
                    <div class="roi-value" id="roi-savings">4,500 ج.م</div>
                </div>
            </div>
        </div>
    </section>
"""

js_code = """
    <script>
        // POS Demo Logic
        let receiptTotal = 0;
        function addToReceipt(name, price) {
            const container = document.getElementById('receipt-items-container');
            const emptyMsg = document.getElementById('empty-receipt-msg');
            const totalEl = document.getElementById('receipt-total-value');
            const flash = document.getElementById('demo-flash');
            
            if(emptyMsg) emptyMsg.style.display = 'none';
            
            // Visual flash
            flash.style.animation = 'none';
            void flash.offsetWidth; // trigger reflow
            flash.style.animation = 'flash 0.3s ease-out';
            
            // Add item
            const item = document.createElement('div');
            item.className = 'receipt-line';
            item.innerHTML = `<span>${name}</span><span>${price} ج.م</span>`;
            container.appendChild(item);
            
            // Scroll to bottom
            container.scrollTop = container.scrollHeight;
            
            // Update total
            receiptTotal += price;
            totalEl.innerText = receiptTotal.toLocaleString() + ' ج.م';
        }
        
        function clearReceipt() {
            const container = document.getElementById('receipt-items-container');
            const totalEl = document.getElementById('receipt-total-value');
            
            container.innerHTML = '<div id="empty-receipt-msg" style="text-align: center; margin-top: 50px; color: #999;">اضغط على أي صنف لإضافته للفاتورة</div>';
            receiptTotal = 0;
            totalEl.innerText = '0 ج.م';
        }

        // ROI Calculator Logic
        function calculateROI() {
            const slider = document.getElementById('roiSlider');
            const label = document.getElementById('daily-sales-label');
            const savings = document.getElementById('roi-savings');
            
            const dailySales = parseInt(slider.value);
            label.innerText = dailySales.toLocaleString();
            
            // Assuming 3% daily loss/theft/errors, times 30 days = monthly savings
            const monthlySavings = dailySales * 0.03 * 30;
            savings.innerText = monthlySavings.toLocaleString() + ' ج.م';
        }
    </script>
"""

content = content.replace('<!-- Hardware Compatibility -->', pos_demo_html + '\n    <!-- Hardware Compatibility -->')
content = content.replace('<!-- Pricing Strategy -->', roi_calc_html + '\n    <!-- Pricing Strategy -->')
content = content.replace('</body>', js_code + '\n</body>')

with open('LandingPage/index.html', 'w', encoding='utf-8') as f:
    f.write(content)
print('Updated index.html successfully.')
