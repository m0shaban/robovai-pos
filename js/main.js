// RoboVAI PRO POS - Advanced Animations & AIO Scripts

document.addEventListener('DOMContentLoaded', () => {
    
    // --- Navbar Scroll Effect ---
    const navbar = document.querySelector('.navbar');
    window.addEventListener('scroll', () => {
        if (window.scrollY > 50) {
            navbar.classList.add('scrolled');
        } else {
            navbar.classList.remove('scrolled');
        }
    });

    // --- Smooth Scroll for Anchor Links ---
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            const targetId = this.getAttribute('href');
            if(targetId === '#') return;
            
            const targetElement = document.querySelector(targetId);
            if(targetElement) {
                const headerOffset = 80;
                const elementPosition = targetElement.getBoundingClientRect().top;
                const offsetPosition = elementPosition + window.pageYOffset - headerOffset;
                
                window.scrollTo({
                    top: offsetPosition,
                    behavior: 'smooth'
                });
            }
        });
    });

    // --- Parallax for Huge Outline Text ---
    const parallaxText = document.getElementById('parallax-text');
    window.addEventListener('scroll', () => {
        let scrollPosition = window.pageYOffset;
        if(parallaxText) {
            // Move text upwards slightly faster than scroll to create depth
            parallaxText.style.transform = `translate(-50%, calc(-50% - ${scrollPosition * 0.4}px))`;
        }
    });

    // --- Bento Box Glow Tracking Effect ---
    const bentoCards = document.querySelectorAll('.bento-card');
    const bentoSection = document.getElementById('bento');
    
    if(bentoSection) {
        bentoSection.addEventListener('mousemove', e => {
            for(const card of bentoCards) {
                const rect = card.getBoundingClientRect();
                const x = e.clientX - rect.left;
                const y = e.clientY - rect.top;

                card.style.setProperty('--mouse-x', `${x}px`);
                card.style.setProperty('--mouse-y', `${y}px`);
            }
        });
    }

    // --- Intersection Observer for Blur/Fade Reveal Animations ---
    const revealElements = document.querySelectorAll('.reveal-blur');
    
    const revealOptions = {
        root: null,
        rootMargin: '0px 0px -50px 0px', // Trigger slightly before it comes into view
        threshold: 0.1 
    };

    const revealObserver = new IntersectionObserver((entries, observer) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('is-revealed');
                // Optional: stop observing once revealed
                observer.unobserve(entry.target);
            }
        });
    }, revealOptions);

    revealElements.forEach(el => {
        revealObserver.observe(el);
    });

    // Log initialization for AIO metrics tracking
    console.log('%c RoboVAI POS Extreme UI & AIO Loaded \u2705', 'color: #00ffcc; font-weight: bold; font-size: 14px; background: #111; padding: 5px; border-radius: 5px;');
});
