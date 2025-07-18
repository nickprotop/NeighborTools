// NeighborTools Landing Page Interactive Features

document.addEventListener('DOMContentLoaded', function() {
    // Smooth scrolling for navigation links
    const scrollLinks = document.querySelectorAll('a[href^="#"]');
    
    scrollLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            e.preventDefault();
            
            const targetId = this.getAttribute('href');
            const targetSection = document.querySelector(targetId);
            
            if (targetSection) {
                const offsetTop = targetSection.offsetTop - 80; // Account for fixed nav
                
                window.scrollTo({
                    top: offsetTop,
                    behavior: 'smooth'
                });
            }
        });
    });

    // Navigation background opacity on scroll
    const nav = document.querySelector('.nav');
    
    function updateNavBackground() {
        const scrollY = window.scrollY;
        const opacity = Math.min(scrollY / 100, 1);
        
        if (scrollY > 50) {
            nav.style.background = `rgba(255, 255, 255, ${0.95 + opacity * 0.05})`;
            nav.style.borderBottomColor = `rgba(226, 232, 240, ${opacity})`;
        } else {
            nav.style.background = 'rgba(255, 255, 255, 0.95)';
            nav.style.borderBottomColor = 'rgba(226, 232, 240, 0.5)';
        }
    }

    window.addEventListener('scroll', updateNavBackground);

    // Intersection Observer for fade-in animations
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };

    const observer = new IntersectionObserver(function(entries) {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.style.animationPlayState = 'running';
                entry.target.classList.add('animate-in');
            }
        });
    }, observerOptions);

    // Observe feature cards and sections
    const animatedElements = document.querySelectorAll('.feature-card, .impact-text, .cta-content');
    animatedElements.forEach(el => {
        el.style.animationPlayState = 'paused';
        observer.observe(el);
    });

    // Parallax effect for hero background
    const heroGraphic = document.querySelector('.hero-graphic');
    
    function updateParallax() {
        const scrollY = window.scrollY;
        const rate = scrollY * -0.5;
        
        if (heroGraphic) {
            heroGraphic.style.transform = `translateY(${rate}px)`;
        }
    }

    window.addEventListener('scroll', updateParallax);

    // Stats counter animation
    const stats = document.querySelectorAll('.stat-number');
    const statsSection = document.querySelector('.impact');
    
    let statsAnimated = false;
    
    function animateStats() {
        if (statsAnimated) return;
        
        stats.forEach(stat => {
            const finalValue = stat.textContent;
            const numericValue = parseInt(finalValue.replace(/\D/g, ''));
            const suffix = finalValue.replace(/\d/g, '');
            
            let currentValue = 0;
            const increment = numericValue / 30; // Animate over 30 frames
            
            const counter = setInterval(() => {
                currentValue += increment;
                
                if (currentValue >= numericValue) {
                    currentValue = numericValue;
                    clearInterval(counter);
                }
                
                stat.textContent = Math.floor(currentValue) + suffix;
            }, 50);
        });
        
        statsAnimated = true;
    }

    // Trigger stats animation when section is visible
    const statsObserver = new IntersectionObserver(function(entries) {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                animateStats();
            }
        });
    }, { threshold: 0.3 });

    if (statsSection) {
        statsObserver.observe(statsSection);
    }

    // Button hover effects
    const buttons = document.querySelectorAll('.btn');
    
    buttons.forEach(button => {
        button.addEventListener('mouseenter', function() {
            this.style.transform = 'translateY(-2px)';
        });
        
        button.addEventListener('mouseleave', function() {
            this.style.transform = 'translateY(0)';
        });
    });

    // Mobile menu toggle (if needed for future expansion)
    const navBrand = document.querySelector('.nav-brand');
    
    navBrand.addEventListener('click', function() {
        // Scroll to top
        window.scrollTo({
            top: 0,
            behavior: 'smooth'
        });
    });

    // Keyboard navigation support
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Enter' || e.key === ' ') {
            const focusedElement = document.activeElement;
            
            if (focusedElement.classList.contains('btn') || focusedElement.tagName === 'A') {
                e.preventDefault();
                focusedElement.click();
            }
        }
    });

    // Performance optimization: throttle scroll events
    let ticking = false;
    
    function requestTick() {
        if (!ticking) {
            requestAnimationFrame(function() {
                updateNavBackground();
                updateParallax();
                ticking = false;
            });
            ticking = true;
        }
    }

    window.addEventListener('scroll', requestTick);

    console.log('🚀 NeighborTools landing page loaded successfully');
});