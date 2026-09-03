let deferredPrompt;

// Detects if device is on iOS 
const isIos = () => {
    const userAgent = window.navigator.userAgent.toLowerCase();
    return /iphone|ipad|ipod/.test(userAgent);
}

// Detects if device is in standalone mode
const isInStandaloneMode = () => ('standalone' in window.navigator) && (window.navigator.standalone);

window.addEventListener('beforeinstallprompt', (e) => {
    e.preventDefault();
    deferredPrompt = e;
    
    // Automatically show install promotion after a delay if not prompted before
    setTimeout(() => {
        const hasPrompted = sessionStorage.getItem('robovai_install_prompted');
        if (!hasPrompted && deferredPrompt) {
             triggerInstallPrompt();
        }
    }, 5000);
});

window.addEventListener('appinstalled', () => {
    deferredPrompt = null;
    console.log('PWA was installed');
    Swal.fire({
        title: '🎉 شكراً لك!',
        text: 'تم تثبيت Smart Inventory Pro بنجاح. الآن يمكنك العمل بكفاءة عالية وبدون إنترنت.',
        icon: 'success',
        confirmButtonText: 'رائع',
        background: 'rgba(30, 41, 59, 0.9)',
        color: '#f8fafc',
    });
});

window.triggerInstallPrompt = async () => {
    if (deferredPrompt) {
        Swal.fire({
            title: '✨ تجربة أفضل بانتظارك',
            text: 'للحصول على أداء أسرع وعمل بدون إنترنت (Offline)، قم بتثبيت تطبيق Smart Inventory Pro الآن!',
            icon: 'info',
            showCancelButton: true,
            confirmButtonText: 'تثبيت التطبيق',
            cancelButtonText: 'لاحقاً',
            background: 'rgba(30, 41, 59, 0.9)',
            color: '#f8fafc',
            customClass: {
                popup: 'glass-popup'
            }
        }).then(async (result) => {
            if (result.isConfirmed) {
                deferredPrompt.prompt();
                const { outcome } = await deferredPrompt.userChoice;
                console.log(`User response to the install prompt: ${outcome}`);
                deferredPrompt = null;
            }
            sessionStorage.setItem('robovai_install_prompted', 'true');
        });
        
    } else if (isIos() && !isInStandaloneMode()) {
        Swal.fire({
            title: '🍏 مستخدم آبل؟',
            html: `لتثبيت التطبيق على الآيفون:<br><br>1. اضغط على أيقونة <b>المشاركة</b> بالأسفل <br>2. ثم اختر <b>إضافة للشاشة الرئيسية</b> 📱`,
            icon: 'info',
            confirmButtonText: 'حسناً فهمت',
            background: 'rgba(30, 41, 59, 0.9)',
            color: '#f8fafc',
            customClass: {
                popup: 'glass-popup'
            }
        });
    } else {
        Swal.fire({
             title: 'معلومة',
             text: 'التطبيق مثبت بالفعل على جهازك أو لا يمكن تثبيته من هذا المتصفح.',
             icon: 'info',
             background: 'rgba(30, 41, 59, 0.9)',
             color: '#f8fafc'
         });
    }
}

// Show specific prompt for iOS automatically once
window.addEventListener('load', () => {
    setTimeout(() => {
        if (isIos() && !isInStandaloneMode() && !sessionStorage.getItem('robovai_ios_prompted')) {
            window.triggerInstallPrompt();
            sessionStorage.setItem('robovai_ios_prompted', 'true');
        }
    }, 5000);
});
