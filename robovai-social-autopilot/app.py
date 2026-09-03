import os
import streamlit as st
from PIL import Image
from dotenv import load_dotenv

# Load env variables
load_dotenv()

from ai_generator import generate_social_content
from content_calendar import SCHEDULED_CAMPAIGNS, ASSETS_DIR
from publishers.telegram_pub import publish_to_telegram
from publishers.meta_pub import publish_to_facebook
from publishers.twitter_pub import publish_to_twitter

# --- Page Configuration ---
st.set_page_config(
    page_title="RoboVAI Social Autopilot 🚀",
    page_icon="🤖",
    layout="wide",
    initial_sidebar_state="expanded"
)

# --- Custom RTL & Glassmorphism Styling ---
st.markdown("""
<style>
    @import url('https://fonts.googleapis.com/css2?family=Tajawal:wght@400;500;700;900&display=swap');
    
    html, body, [class*="css"] {
        font-family: 'Tajawal', sans-serif;
        direction: rtl;
        text-align: right;
    }
    .main-title {
        font-size: 2.3rem;
        font-weight: 900;
        color: #38bdf8;
        margin-bottom: 5px;
    }
    .sub-title {
        color: #94a3b8;
        font-size: 1.05rem;
        margin-bottom: 25px;
    }
    .platform-card {
        background: rgba(15, 23, 42, 0.6);
        border: 1px solid rgba(56, 189, 248, 0.2);
        border-radius: 12px;
        padding: 16px;
        margin-bottom: 15px;
    }
    .badge-status-on {
        background: #065f46;
        color: #34d399;
        padding: 3px 8px;
        border-radius: 6px;
        font-size: 0.8rem;
    }
    .badge-status-off {
        background: #7f1d1d;
        color: #f87171;
        padding: 3px 8px;
        border-radius: 6px;
        font-size: 0.8rem;
    }
</style>
""", unsafe_allow_html=True)

# --- Sidebar: Status & Settings ---
st.sidebar.markdown("### ⚙️ حالة الربط والمفاتيح")

gemini_ok = bool(os.getenv("GEMINI_API_KEY"))
grok_ok = bool(os.getenv("GROK_API_KEY"))
tg_ok = bool(os.getenv("TELEGRAM_BOT_TOKEN") and os.getenv("TELEGRAM_CHANNEL_ID"))
fb_ok = bool(os.getenv("FB_PAGE_ACCESS_TOKEN"))
tw_ok = bool(os.getenv("TWITTER_API_KEY"))

st.sidebar.markdown(f"🤖 **Google Gemini (مجاني):** {'<span class=\"badge-status-on\">متصل ✅</span>' if gemini_ok else '<span class=\"badge-status-off\">غير مضبوط</span>'}", unsafe_allow_html=True)
st.sidebar.markdown(f"🧠 **Grok xAI:** {'<span class=\"badge-status-on\">متصل ✅</span>' if grok_ok else '<span class=\"badge-status-off\">غير مضبوط</span>'}", unsafe_allow_html=True)
st.sidebar.markdown(f"📢 **قناة تليجرام:** {'<span class=\"badge-status-on\">جاهزة للنشر ✅</span>' if tg_ok else '<span class=\"badge-status-off\">غير مضبوطة</span>'}", unsafe_allow_html=True)
st.sidebar.markdown(f"📘 **Facebook:** {'<span class=\"badge-status-on\">جاهز للنشر ✅</span>' if fb_ok else '<span class=\"badge-status-off\">غير مضبوط</span>'}", unsafe_allow_html=True)
st.sidebar.markdown(f"🐦 **X (تويتر):** {'<span class=\"badge-status-on\">جاهز للنشر ✅</span>' if tw_ok else '<span class=\"badge-status-off\">غير مضبوط</span>'}", unsafe_allow_html=True)

st.sidebar.markdown("---")
ai_choice = st.sidebar.selectbox("محرك الذكاء الاصطناعي المفضل:", ["تلقائي (Gemini أو Grok)", "Gemini 2.0 Flash (مجاني 100%)", "Grok 2 (xAI)"])
engine_code = "gemini" if "Gemini" in ai_choice else ("grok" if "Grok" in ai_choice else "auto")

# --- Main App Header ---
st.markdown("<div class=\"main-title\">🚀 RoboVAI Social Media Autopilot</div>", unsafe_allow_html=True)
st.markdown("<div class=\"sub-title\">المنظومة الذكية لإدارة ونشر المحتوى التسويقي تلقائياً لجلب عملاء لكاشير RoboVAI PRO POS v6.0</div>", unsafe_allow_html=True)

# Tabs
tab1, tab2, tab3 = st.tabs(["✨ توليد ونشر محتوى جديد", "📅 جدول الحملات الجاهزة", "📖 دليل ضبط المفاتيح مجاناً"])

# ================= TAB 1: GENERATE & PUBLISH =================
with tab1:
    col_left, col_right = st.columns([1.2, 1])

    with col_left:
        st.markdown("#### 1. موضوع المنشور")
        
        # Topic selection
        topic_mode = st.radio("مصدر الفكرة:", ["اختر من الحملات المقترحة", "أدخل فكرة مخصصة"], horizontal=True)
        if topic_mode == "اختر من الحملات المقترحة":
            campaign_names = [f"[{c['category']}] {c['title']}" for c in SCHEDULED_CAMPAIGNS]
            selected_idx = st.selectbox("الحملة:", range(len(campaign_names)), format_func=lambda i: campaign_names[i])
            selected_campaign = SCHEDULED_CAMPAIGNS[selected_idx]
            topic_text = selected_campaign["title"] + " - " + selected_campaign["description"]
            default_img_path = selected_campaign["image"]
        else:
            topic_text = st.text_area("اكتب فكرة البوست أو العرض:", "عجز الخزينة وسرقة الكاشير وكيف يمنع كاشير RoboVAI التلاعب عبر تقفيل Z-Report الدقيق مع كود خصم LAUNCH100", height=100)
            default_img_path = None

        st.markdown("#### 2. الصورة المرفقة")
        img_source = st.radio("مصدر الصورة:", ["من مكتبة شاشات النظام", "رفع صورة من جهازي"], horizontal=True)
        
        chosen_image_path = None
        if img_source == "من مكتبة شاشات النظام":
            if os.path.exists(ASSETS_DIR):
                asset_files = [f for f in os.listdir(ASSETS_DIR) if f.endswith(('.png', '.jpeg', '.jpg'))]
                selected_asset = st.selectbox("اختر الشاشة:", asset_files, index=asset_files.index("hero.jpeg") if "hero.jpeg" in asset_files else 0)
                chosen_image_path = os.path.join(ASSETS_DIR, selected_asset)
            else:
                st.warning("مجلد الصور غير موجود.")
        else:
            uploaded_file = st.file_uploader("ارفع صورة للمنشور:", type=['png', 'jpg', 'jpeg'])
            if uploaded_file:
                temp_path = os.path.join(os.path.dirname(__file__), "temp_upload.png")
                with open(temp_path, "wb") as f:
                    f.write(uploaded_file.getbuffer())
                chosen_image_path = temp_path

        generate_btn = st.button("✨ توليد المحتوى الذكي بالذكاء الاصطناعي", type="primary", use_container_width=True)

    with col_right:
        st.markdown("#### معاينة الصورة المحددة")
        if chosen_image_path and os.path.exists(chosen_image_path):
            st.image(chosen_image_path, use_container_width=True)
        else:
            st.info("لم يتم تحديد صورة بعد.")

    # Content generation logic
    if generate_btn:
        with st.spinner("جارٍ صياغة المحتوى عبر الذكاء الاصطناعي..."):
            res = generate_social_content(topic_text, engine=engine_code)
            st.session_state["generated_posts"] = res
            st.session_state["chosen_image"] = chosen_image_path
            st.success("تم توليد المحتوى بنجاح لجميع المنصات! 🎉")

    # Display Generated Content & Publishing Controls
    if "generated_posts" in st.session_state:
        posts = st.session_state["generated_posts"]
        active_img = st.session_state.get("chosen_image")

        st.markdown("---")
        st.markdown("### 📢 معاينة المنشورات وجاهزية النشر:")

        p_col1, p_col2 = st.columns(2)

        # Facebook Preview
        with p_col1:
            st.markdown("#### 📘 فيسبوك (Facebook)")
            fb_text = st.text_area("نص فيسبوك (يمكنك التعديل قبل النشر):", posts.get("facebook", ""), height=220, key="fb_text")
            if st.button("🚀 انشر الآن على Facebook", use_container_width=True):
                with st.spinner("جارٍ النشر على فيسبوك..."):
                    fb_res = publish_to_facebook(fb_text, active_img)
                    if fb_res.get("success"):
                        st.success(f"تم النشر بنجاح على فيسبوك! ID: {fb_res.get('post_id')}")
                    else:
                        st.error(f"تعذر النشر: {fb_res.get('error')}")

        # Telegram Preview
        with p_col2:
            st.markdown("#### 📢 قناة ومجموعات تليجرام")
            tg_text = st.text_area("نص تليجرام:", posts.get("telegram", ""), height=220, key="tg_text")
            if st.button("🚀 انشر الآن على تليجرام", type="primary", use_container_width=True):
                with st.spinner("جارٍ البث على قناة تليجرام..."):
                    tg_res = publish_to_telegram(tg_text, active_img)
                    if tg_res.get("success"):
                        st.success(f"تم البث بنجاح على تليجرام! Message ID: {tg_res.get('message_id')}")
                    else:
                        st.error(f"تعذر النشر: {tg_res.get('error')}")

        p_col3, p_col4 = st.columns(2)

        # Twitter / X Preview
        with p_col3:
            st.markdown("#### 🐦 منصة X (Twitter)")
            tw_text = st.text_area("تغريدة X:", posts.get("twitter", ""), height=150, key="tw_text")
            if st.button("🚀 انشر الآن على X", use_container_width=True):
                with st.spinner("جارٍ النشر على X..."):
                    tw_res = publish_to_twitter(tw_text)
                    if tw_res.get("success"):
                        st.success(f"تم التغريد بنجاح! Tweet ID: {tw_res.get('tweet_id')}")
                    else:
                        st.error(f"تعذر النشر: {tw_res.get('error')}")

        # Instagram Preview
        with p_col4:
            st.markdown("#### 📸 إنستجرام (Instagram)")
            ig_text = st.text_area("كابشن إنستجرام:", posts.get("instagram", ""), height=150, key="ig_text")
            st.info("💡 ملاحظة: النشر التلقائي على إنستجرام يتطلب رابط صورة مباشر على الويب أو النشر اليدوي عبر نسخ النص أعلاه.")

        st.markdown("---")
        if st.button("⚡🚀 انشر الآن في جميع المنصات دفعة واحدة (All-in-One Publish)", use_container_width=True):
            st.info("جارٍ الإرسال لكافة المنصات النشطة...")
            # Publish TG
            tg_res = publish_to_telegram(tg_text, active_img)
            # Publish FB
            fb_res = publish_to_facebook(fb_text, active_img)
            # Publish Twitter
            tw_res = publish_to_twitter(tw_text)
            
            st.write(f"• تليجرام: {'✅ تم' if tg_res.get('success') else '❌ ' + tg_res.get('error', '')}")
            st.write(f"• فيسبوك: {'✅ تم' if fb_res.get('success') else '❌ ' + fb_res.get('error', '')}")
            st.write(f"• إكس (تويتر): {'✅ تم' if tw_res.get('success') else '❌ ' + tw_res.get('error', '')}")

# ================= TAB 2: CONTENT CALENDAR =================
with tab2:
    st.markdown("### 📅 جدول الحملات التسويقية المعدة مسبقاً")
    st.markdown("أفكار محتوى جاهزة تركز على نقاط ألم العملاء وتبرز شاشات النظام الـ 18 وميزة الأوفلاين:")
    
    for c in SCHEDULED_CAMPAIGNS:
        with st.expander(f"📍 [{c['category']}] {c['title']}"):
            col_c1, col_c2 = st.columns([2, 1])
            with col_c1:
                st.write(f"**الوصف التسويقي:** {c['description']}")
                st.write(f"**الصورة المخصصة:** `{os.path.basename(c['image'])}`")
                if st.button(f"استخدام هذه الحملة للتوليد ⚡", key=f"btn_c_{c['id']}"):
                    st.session_state["quick_topic"] = c['title'] + " - " + c['description']
                    st.session_state["quick_img"] = c['image']
                    st.success("تم اختيار الحملة! توجه إلى التبويب الأول واضغط 'توليد المحتوى'.")
            with col_c2:
                if os.path.exists(c['image']):
                    st.image(c['image'], width=240)

# ================= TAB 3: FREE SETUP GUIDE =================
with tab3:
    st.markdown("### 🛠️ كيفية الحصول على المفاتيح المجانية 100% في 5 دقائق")
    
    st.markdown("""
    #### 1. مفتاح Google Gemini (مجاني تماماً 100% للأبد):
    1. ادخل على: [Google AI Studio](https://aistudio.google.com/).
    2. سجل بحساب جوجل واضغط **Get API Key**.
    3. انسخ المفتاح وضعه في ملف `.env` أمام: `GEMINI_API_KEY=your_key`.
    
    #### 2. بوت تليجرام وقناة العروض (مجاني 100% بدون أي حدود):
    1. افتح تليجرام وابحث عن `@BotFather`.
    2. اكتب أمر `/newbot` واختر اسماً ومعرفاً للبوت، سيعطيك رمز `Token`.
    3. أنشئ قناة تليجرام جديدة باسم شركتك (مثلاً: `@robovai_pos`) وأضف البوت فيها كـ **Admin**.
    4. ضع التوكن في `.env` أمام: `TELEGRAM_BOT_TOKEN=...` ومعرف القناة أمام `TELEGRAM_CHANNEL_ID=@your_channel`.
    
    #### 3. فيسبوك وإنستجرام (Meta Graph API):
    1. ادخل على [Meta for Developers](https://developers.facebook.com/).
    2. أنشئ تطبيقاً مجانياً واربطه بصفحة الفيسبوك الخاصة بك للحصول على `Page Access Token`.
    """)
