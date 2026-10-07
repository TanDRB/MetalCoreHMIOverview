// Metalcore HMI – đồng hồ (nếu có trên trang) và dropdown chọn giao diện
(function () {
    'use strict';

    var pad = function (n) { return String(n).padStart(2, '0'); };

    function tickClock() {
        var dateEl = document.getElementById('clock-date');
        var timeEl = document.getElementById('clock-time');
        if (!dateEl && !timeEl) return;
        var d = new Date();
        if (dateEl) dateEl.textContent = d.getFullYear() + '/' + pad(d.getMonth() + 1) + '/' + pad(d.getDate());
        if (timeEl) timeEl.textContent = pad(d.getHours()) + ':' + pad(d.getMinutes()) + ':' + pad(d.getSeconds());
    }
    tickClock();
    setInterval(tickClock, 1000);

    var menu = document.getElementById('screen-menu');
    if (menu) {
        var btn = menu.querySelector('.screen-menu-btn');
        var setOpen = function (open) {
            menu.classList.toggle('open', open);
            btn.setAttribute('aria-expanded', open ? 'true' : 'false');
        };
        btn.addEventListener('click', function (e) {
            e.stopPropagation();
            setOpen(!menu.classList.contains('open'));
        });
        document.addEventListener('click', function (e) { if (!menu.contains(e.target)) setOpen(false); });
        document.addEventListener('keydown', function (e) { if (e.key === 'Escape') setOpen(false); });
    }
})();
