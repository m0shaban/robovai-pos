# 📚 Smart POS - دليل البدء (Al‑Atmani 2026)

> ملاحظة: هذا المستودع يحتوي على ملفات قديمة باسم “Space Edition / SpaceTheme”. التصميم الافتراضي الحالي للبرنامج هو **Al‑Atmani 2026** (وضع داكن + Glass/Acrylic + Bento Dashboard + Touch‑First POS).

> ملاحظة (فبراير 2026): `Themes/SpaceTheme.xaml` لم يعد يتم دمجه افتراضيًا داخل `App.xaml` لتجنب “Legacy bleed”. يمكن تشغيله يدويًا من `src/SmartPOS.WPF/appsettings.json` عبر `Ui:EnableLegacySpaceTheme: true`.

## 🎯 ابدأ من هنا

### الملفات الأساسية (حسب الأولوية)

#### 1️⃣ للبدء الفوري ⚡

**[QUICK_START.md](QUICK_START.md)** - دليل البدء السريع

- خطوات التطبيق (1-8)
- أكواد Services كاملة
- أمثلة XAML جاهزة
- Checklist للمتابعة

#### 2️⃣ للفهم الشامل 📖

**[README_SPACE_POS.md](README_SPACE_POS.md)** - الدليل الكامل (Legacy Space Edition)

- نظرة عامة على النظام
- شرح كل ميزة بالتفصيل
- هيكل المشروع
- اختصارات لوحة المفاتيح

#### 3️⃣ للمقارنة 🔄

**[BEFORE_AFTER_COMPARISON.md](BEFORE_AFTER_COMPARISON.md)** - قبل وبعد (مع ملاحظة التحديثات الحديثة)

- الفرق البصري
- مقارنة الأرقام
- تأثير الأعمال

#### 4️⃣ للإنجازات ✅

**[COMPLETED.md](COMPLETED.md)** - ملخص الإنجازات

- الملفات المنشأة
- الإحصائيات
- Build Status

---

## 📦 الملفات المنشأة

### النماذج (7 ملفات)

```
Core/Entities/
├── ⭐ Shift.cs
├── ⭐ CustomerLoyalty.cs
├── ⭐ LoyaltyTransaction.cs
├── ⭐ Return.cs
├── ⭐ ReturnDetail.cs
├── ✏️ Customer.cs
└── ✏️ Sale.cs
```

### التصميم (2 ملفات)

```
WPF/
├── Themes/SpaceTheme.xaml ⭐ (Legacy/اختياري)
└── App.xaml ✏️ (Al‑Atmani 2026 هو الافتراضي)
```

### التوثيق (5 ملفات)

```
Root/
├── README_SPACE_POS.md ⭐
├── QUICK_START.md ⭐
├── COMPLETED.md ⭐
├── BEFORE_AFTER_COMPARISON.md ⭐
└── START_HERE.md ⭐ (هذا الملف)
```

---

## 🚀 خطوات البدء السريعة

### 1. تحديث قاعدة البيانات

```bash
cd src/SmartPOS.Infrastructure
dotnet ef migrations add AddSpacePOS
dotnet ef database update
```

### 2. إضافة المكتبات

```bash
cd ../SmartPOS.WPF
dotnet add package QRCoder
dotnet add package LiveCharts.Wpf
dotnet add package ClosedXML
```

### 3. البناء والتشغيل

```bash
dotnet build
dotnet run
```

**التفاصيل الكاملة:** راجع [QUICK_START.md](QUICK_START.md)

---

## 🌟 الميزات الرئيسية

| الميزة             | الملف                              | التوثيق                    |
| ------------------ | ---------------------------------- | -------------------------- |
| 🛡️ الورديات        | `Shift.cs`                         | QUICK_START.md             |
| 💎 نقاط الولاء     | `CustomerLoyalty.cs`               | README_SPACE_POS.md        |
| 🔄 المرتجعات       | `Return.cs`                        | BEFORE_AFTER_COMPARISON.md |
| 📄 QR Code         | `Sale.cs`                          | QUICK_START.md             |
| 🌌 التصميم الفضائي | `SpaceTheme.xaml` (Legacy/اختياري) | README_SPACE_POS.md        |

---

## 💡 أسئلة شائعة

**س: من أين أبدأ؟**
ج: افتح [QUICK_START.md](QUICK_START.md) واتبع الخطوات

**س: أين الكود الكامل؟**
ج: في QUICK_START.md (الخطوات 4-6)

**س: هل يجب تطبيق كل شيء؟**
ج: لا، اختر ما تحتاجه فقط

**س: كيف أختبر؟**
ج: `dotnet build && dotnet run`

---

## 📊 الحالة

```
✅ Build: Successful
⚠️ Warnings: 0
❌ Errors: 0
📦 Entities: +5
🎨 Styles: +8
📝 Docs: 1500+ lines
```

---

**🎊 جاهز للانطلاق! افتح [QUICK_START.md](QUICK_START.md) الآن 🚀**
