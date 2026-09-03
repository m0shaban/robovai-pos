import os
import json
import requests
from dotenv import load_dotenv

load_dotenv()

SYSTEM_PROMPT = """أنت خبير تسويق رقمي ومدير محتوى سوشيال ميديا محترف متخصص في أنظمة نقاط البيع (POS) وتكنولوجيا إدارة المتاجر والمطاعم في مصر والسعودية والخليج.
هدفك: كتابة منشورات تسويقية ذكية ومقنعة تمس المشاكل اليومية الحقيقية لأصحاب الأنشطة التجارية (سرقة الكاشير، عجز الخزينة، انقطاع النت وتعطل الزبائن، ضياع المخزون، استنزاف الاشتراكات الشهرية).

معلومات نظام RoboVAI PRO POS v6.0:
- يعمل 100% أوفلاين مدى الحياة بدون أي حاجة للإنترنت.
- ترخيص دائم بدون اشتراكات شهرية متكررة.
- إنهاء الفاتورة في ثانيتين بدون أي تهنيج (0% Lag).
- بوت تليجرام تلقائي يرسل إشعارات بالمبيعات وتقفيل الوردية Z-Report للمالك على الموبايل لحظياً.
- تطبيق مخازن PWA مجاني يجرد البضاعة بكاميرا الهاتف بدون أجهزة هاند هيلد باهظة.
- لوحة تحكم إدارية بالمتصفح للمبيعات وساعات الذروة.
- متوافق مع الفاتورة الإلكترونية والضريبية في مصر وهيئة الزكاة والضريبة ZATCA بالسعودية.
- عروض الإطلاق: كود الخصم الحصري (LAUNCH100) لأول 100 عميل، مع تجربة مجانية 14 يوماً.
- رابط الموقع: https://pos.robovai.tech/
- واتساب المبيعات: +201121891913
"""

def generate_social_content(topic, target_platform="all", custom_image_description="", engine="auto"):
    """
    Tries to generate social content using Grok API or Google Gemini API.
    Returns a dict with posts for: facebook, twitter, instagram, telegram.
    """
    grok_key = os.getenv("GROK_API_KEY", "").strip()
    gemini_key = os.getenv("GEMINI_API_KEY", "").strip()

    prompt = f"""
قم بصياغة محتوى تسويقي احترافي وجذاب حول موضوع: "{topic}"
{f"الصورة المرفقة تعرض: {custom_image_description}" if custom_image_description else ""}

المطلوب: توليد 4 نصوص مخصصة بصيغة JSON حصراً، على النحو التالي:
{{
  "facebook": "نص فيسبوك جذاب بأسلوب القصة (Storytelling) يركز على المشكلة والحل، مع روابط وهاشتاجات وكود LAUNCH100 ورابط الموقع pos.robovai.tech",
  "twitter": "تغريدة تويتر (X) قوية وموجزة ومباشرة تثير الفضول مع هاشتاجين ورابط الموقع",
  "instagram": "كابشن إنستجرام أنيق بالنقاط التوضيحية والإيموجي مع هاشتاجات تجارية نشطة",
  "telegram": "رسالة قناة تليجرام موجهة لأصحاب الأعمال مع عناوين عريضة ورابط واتساب وكود LAUNCH100"
}}

هام جداً: أجب بكائن الـ JSON فقط بدون أي مقدمات أو علامات إضافية خارج الـ JSON.
"""

    # 1. Try Grok if preferred or available
    if (engine == "grok" or (engine == "auto" and grok_key)) and grok_key:
        try:
            res = requests.post(
                "https://api.x.ai/v1/chat/completions",
                headers={
                    "Authorization": f"Bearer {grok_key}",
                    "Content-Type": "application/json"
                },
                json={
                    "model": "grok-2",
                    "messages": [
                        {"role": "system", "content": SYSTEM_PROMPT},
                        {"role": "user", "content": prompt}
                    ],
                    "temperature": 0.7
                },
                timeout=30
            )
            if res.status_code == 200:
                raw = res.json()["choices"][0]["message"]["content"]
                return parse_json_response(raw)
        except Exception as e:
            print(f"[Grok Error]: {e}")

    # 2. Try Gemini (Free Tier)
    if gemini_key:
        try:
            url = f"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={gemini_key}"
            res = requests.post(
                url,
                headers={"Content-Type": "application/json"},
                json={
                    "contents": [{
                        "parts": [
                            {"text": SYSTEM_PROMPT + "\n\n" + prompt}
                        ]
                    }],
                    "generationConfig": {
                        "temperature": 0.7,
                        "responseMimeType": "application/json"
                    }
                },
                timeout=30
            )
            if res.status_code == 200:
                raw = res.json()["candidates"][0]["content"]["parts"][0]["text"]
                return parse_json_response(raw)
        except Exception as e:
            print(f"[Gemini Error]: {e}")

    # 3. Fallback High-Quality Templates if no API key is set yet
    return fallback_templates(topic)

def parse_json_response(raw_text):
    text = raw_text.strip()
    if text.startswith("```json"):
        text = text[7:]
    if text.startswith("```"):
        text = text[3:]
    if text.endswith("```"):
        text = text[:-3]
    try:
        return json.loads(text.strip())
    except Exception:
        return {
            "facebook": text,
            "twitter": text[:270],
            "instagram": text,
            "telegram": text
        }

def fallback_templates(topic):
    return {
        "facebook": f"🚨 أصحاب المتاجر والمطاعم.. هل تعلم أن 70% من عجز الخزينة يحدث بدون قصد بسبب بطء الكاشير وتراكم الفواتير؟\n\nنظام RoboVAI PRO POS v6.0 صُمم ليحل هذه الأزمة نهائياً:\n✅ يعمل 100% أوفلاين بدون نت\n✅ تقفيل ورديات Z-Report بالسنتيم\n✅ إشعارات حية على تليجرام بكل فاتورة\n\n🔥 خصم الإطلاق متاح لأول 100 عميل فقط بكود (LAUNCH100)!\n🌐 جرب مجاناً 14 يوماً: https://pos.robovai.tech/\n💬 تواصل واتساب: https://wa.me/201121891913",
        "twitter": f"النت فصل وطابور الزبائن واقف؟ ❌\nمع كاشير RoboVAI PRO POS المبيعات مش هتقف دقيقة واحدة لأن النظام يعمل 100% أوفلاين مع تقفيل ورديات Z-Report محكم بالسنتيم ⚡\n\n🔥 كود الخصم: LAUNCH100\n🔗 https://pos.robovai.tech/",
        "instagram": f"ودّع صداع عجز الخزينة ومشاكل الكاشير في أوقات الذروة! 🛒📊\n\nمنظومة RoboVAI PRO POS توفر لك:\n⚡ سرعة خارقة (فاتورة في ثانيتين)\n🔒 حماية تامة للخزينة ومنع التلاعب\n📲 إشعارات لحظية على تليجرام\n📱 جرد المخزن بكاميرا الموبايل\n\n🎁 احصل على خصم الإطلاق بكود: LAUNCH100\nرابط التجربة المجانية في البايو 👆\n\n#كاشير #نقاط_بيع #سوبرماركت #مطاعم #كافيهات #POS #تجارة #مخازن",
        "telegram": f"📢 **تنبيه هام لأصحاب الأنشطة التجارية والمطاعم**\n\nهل تعاني من عجز الخزينة اليومي أو بطء النظام القديم؟\n\nنظام **RoboVAI PRO POS v6.0** يمنحك السيطرة التامة:\n• يعمل 100% بدون إنترنت.\n• ترخيص تمليك دائم مدى الحياة.\n• بوت تليجرام يرسل لك مبيعات محلك أولاً بأول.\n\n🔥 **عرض خاص لأول 100 عميل**: كود خصم إضافي `LAUNCH100`\n\n🌐 للمعاينة والتجربة المجانية (14 يوماً):\nhttps://pos.robovai.tech/\n💬 للتواصل المباشر مع المبيعات:\nhttps://wa.me/201121891913"
    }
