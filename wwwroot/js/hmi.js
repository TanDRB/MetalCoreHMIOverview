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

    var fsBtn = document.getElementById('fs-btn');
    if (fsBtn) {
        var root = document.documentElement;
        var canFs = !!(root.requestFullscreen || root.webkitRequestFullscreen);
        var isFs = function () { return !!(document.fullscreenElement || document.webkitFullscreenElement); };
        var enterFs = function () {
            var p = root.requestFullscreen ? root.requestFullscreen() : root.webkitRequestFullscreen();
            if (p && p.catch) p.catch(function () { /* trình duyệt từ chối: bỏ qua */ });
        };
        var exitFs = function () {
            var p = document.exitFullscreen ? document.exitFullscreen() : document.webkitExitFullscreen();
            if (p && p.catch) p.catch(function () { });
        };
        var KEY = 'hmiFullscreen';
        var store = function (v) { try { localStorage.setItem(KEY, v ? '1' : '0'); } catch (e) { /* bỏ qua */ } };
        var wanted = function () { try { return localStorage.getItem(KEY) === '1'; } catch (e) { return false; } };

        if (!canFs) {
            fsBtn.style.display = 'none';
        } else {
            var sync = function () { fsBtn.classList.toggle('is-fs', isFs()); };
            fsBtn.addEventListener('click', function () {
                if (isFs()) { store(false); exitFs(); } else { store(true); enterFs(); }
            });
            document.addEventListener('fullscreenchange', sync);
            document.addEventListener('webkitfullscreenchange', sync);
            sync();

            // Đổi trang làm trình duyệt thoát toàn màn hình: lần chạm đầu tiên trên trang mới bật lại
            if (wanted() && !isFs()) {
                var rearm = function () {
                    document.removeEventListener('pointerdown', rearm, true);
                    document.removeEventListener('keydown', rearm, true);
                    if (!isFs() && wanted()) enterFs();
                };
                document.addEventListener('pointerdown', rearm, true);
                document.addEventListener('keydown', rearm, true);
            }
        }
    }
})();
