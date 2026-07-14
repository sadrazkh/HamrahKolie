// افکت‌های عمومی صفحه‌ساز: نمایش هنگام اسکرول + شمارندهٔ متحرک اعداد داده‌محور.
(function () {
  document.documentElement.classList.add('pb-js');

  var faDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹'];
  function toFa(n) {
    return Math.round(n).toLocaleString('en-US').replace(/\d/g, function (d) { return faDigits[+d]; });
  }

  function countUp(el) {
    var target = parseFloat(el.getAttribute('data-count-to'));
    if (isNaN(target)) return;
    if (el.dataset.counted) return;
    el.dataset.counted = '1';
    var duration = 1400, start = performance.now();
    function frame(now) {
      var t = Math.min(1, (now - start) / duration);
      var eased = 1 - Math.pow(1 - t, 3);
      el.textContent = toFa(target * eased);
      if (t < 1) requestAnimationFrame(frame); else el.textContent = toFa(target);
    }
    requestAnimationFrame(frame);
  }

  function ready(fn) {
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', fn);
    else fn();
  }

  ready(function () {
    var reveal = document.querySelectorAll('.pb-animate-fade, .pb-animate-slide-up, .pb-animate-zoom');
    var counters = document.querySelectorAll('.stat-value[data-count-to]');

    if (!('IntersectionObserver' in window)) {
      reveal.forEach(function (el) { el.classList.add('pb-in'); });
      counters.forEach(countUp);
      return;
    }

    var revObserver = new IntersectionObserver(function (entries, obs) {
      entries.forEach(function (e) {
        if (e.isIntersecting) { e.target.classList.add('pb-in'); obs.unobserve(e.target); }
      });
    }, { threshold: 0.12 });
    reveal.forEach(function (el) { revObserver.observe(el); });

    var countObserver = new IntersectionObserver(function (entries, obs) {
      entries.forEach(function (e) {
        if (e.isIntersecting) { countUp(e.target); obs.unobserve(e.target); }
      });
    }, { threshold: 0.4 });
    counters.forEach(function (el) { countObserver.observe(el); });
  });
})();
