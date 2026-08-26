# SmartPOS License Web

نسخة ويب خفيفة من أداة توليد أكواد التفعيل.

## التشغيل المحلي

```bash
dotnet run --project tools/SmartPOS.LicenseWeb/SmartPOS.LicenseWeb.csproj
```

ثم افتح:

- `http://localhost:5000` أو المنفذ الذي يظهر في الكونسول.

## الإعدادات (مهم)

لا ترفع المفتاح الخاص داخل الريبو.

استخدم أحد الخيارين:

1. `LICENSE_PRIVATE_KEY_PATH` مسار ملف PEM على السيرفر.
2. `LICENSE_PRIVATE_KEY_PEM` محتوى PEM مباشرة كمتغير بيئة.

إعدادات المسؤول:

- `LICENSE_ADMIN_USER`
- `LICENSE_ADMIN_PASS`

## النشر على Render (مُوصى به)

1. ارفع المشروع على GitHub.
2. من Render اختر New + ثم Blueprint.
3. اختر نفس الريبو؛ Render سيقرأ الملف render.yaml تلقائياً.
4. أضف Environment Variables التالية في Render:
   - LICENSE_ADMIN_USER
   - LICENSE_ADMIN_PASS
   - LICENSE_PRIVATE_KEY_PEM
5. في LICENSE_PRIVATE_KEY_PEM ضع كامل محتوى المفتاح الخاص PEM.
6. اعمل Deploy، وبعد النجاح افتح رابط الخدمة.

ملاحظة: يمكن وضع المفتاح بصيغة أسطر متعددة أو بصيغة \n في سطر واحد.

## نشر أونلاين (عام)

- شغّلها على VPS/Cloud يدعم .NET 8.
- فعل HTTPS فقط.
- احمِ السيرفر بـ Firewall ويفضل IP allowlist.
- لا تشارك بيانات الدخول أو المفتاح الخاص.

## ملاحظة أمنية

هذه واجهة مبدئية (MVP). للبيئة الإنتاجية يفضل إضافة:

- مصادقة أقوى (JWT/OAuth)
- تتبع عمليات (Audit Log)
- Rate limiting
- 2FA
