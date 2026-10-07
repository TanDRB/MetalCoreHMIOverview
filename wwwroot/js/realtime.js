// Metalcore HMI – nhận dữ liệu realtime từ Kepware qua Server-Sent Events (/api/tags/stream)
// và cập nhật các trang Overview, Viscosity, Machine. Không cần thư viện ngoài.
(function () {
    'use strict';

    var cls = document.body.classList;
    var page = cls.contains('ovw') ? 'ovw' : cls.contains('visc') ? 'visc' : cls.contains('mach') ? 'mach' : null;
    if (!page || !window.EventSource) return;

    // Metric: 0 Status, 1 Pressure, 2 Actual, 3 Standard
    var NA = '--';

    function num(v, d) {
        if (v === null || v === undefined || isNaN(v)) return NA;
        return Number(v).toFixed(d === undefined ? 1 : d).replace('.', ',');
    }
    function shortNum(v) {   // 75 -> "75", 16.5 -> "16,5"
        if (v === null || v === undefined || isNaN(v)) return NA;
        return (v % 1 === 0 ? String(v) : Number(v).toFixed(1)).replace('.', ',');
    }
    function $(root, sel) { return root.querySelector(sel); }
    function setText(el, text) { if (el) el.textContent = text; }
    function setLastText(el, text) {          // đổi chữ cuối trong phần tử (giữ nguyên icon)
        if (!el) return;
        var n = el.lastChild;
        if (n && n.nodeType === 3) n.nodeValue = text; else el.appendChild(document.createTextNode(text));
    }
    function setFirstText(el, text) {
        if (!el) return;
        var n = el.firstChild;
        if (n && n.nodeType === 3) n.nodeValue = text; else el.insertBefore(document.createTextNode(text), el.firstChild);
    }
    function setClass(el, base, state) { if (el) el.className = (base + ' ' + state).trim(); }

    function key(m, section, metric) { return m + '|' + section + '|' + metric; }

    function ok(d) { return d && d.value !== null && d.value !== undefined; }   // hiển thị mọi giá trị đọc được, bất kể chất lượng Good/Bad
    function state(d) {                        // 'ok' | 'ng' | 'na'
        if (!ok(d) || !d.result) return 'na';
        return d.result === 'NG' ? 'ng' : 'ok';
    }
    function statusInfo(d) {
        if (!ok(d)) return { text: 'OFFLINE', on: false };
        return d.value >= 1 ? { text: 'START', on: true } : { text: 'STOP', on: false };
    }

    // ---------- Metalcore HMI Overview ----------
    function applyOvw(map) {
        document.querySelectorAll('.t-card').forEach(function (card) {
            var m = +card.dataset.machine;
            var t = map[key(m, 'Temperature', 2)];
            var st = state(t);

            var s = statusInfo(map[key(m, 'Machine', 0)]);
            var start = $(card, '.t-start');
            setClass(start, 't-start', s.on ? '' : 'is-off');
            setLastText(start, s.text);

            if (t && t.standard !== null && t.tolerance !== null) {
                setText($(card, '.t-std-n'), shortNum(t.standard));
                setText($(card, '.t-std-tol'), '(±) ' + shortNum(t.tolerance) + ' ' + (t.unit || '°C'));
            }

            setClass($(card, '.t-act'), 't-act', 't-' + st);
            setText($(card, '.t-act-val b'), ok(t) ? num(t.value, 1) : NA);

            var pill = $(card, '.t-pill');
            setClass(pill, 't-pill', 't-' + st);
            setLastText(pill, st === 'na' ? NA : (st === 'ng' ? 'NG' : 'OK'));
            var use = pill && pill.querySelector('use');
            if (use) use.setAttribute('href', st === 'ng' ? '#i-cross' : '#i-check');
            card.classList.toggle('is-ng', st === 'ng');
        });
    }

    // ---------- Viscosity ----------
    function applyVisc(map) {
        document.querySelectorAll('.v-row').forEach(function (row) {
            var m = +row.dataset.machine;
            var s = statusInfo(map[key(m, 'Machine', 0)]);

            row.querySelectorAll('.v-stage').forEach(function (stage) {
                var sec = stage.classList.contains('s1') ? 'Stage1' : 'Stage2';
                var act = map[key(m, sec, 2)];
                var pre = map[key(m, sec, 1)];
                var st = state(act);
                function cell(label) { return $(stage, '.v-cell[data-label="' + label + '"]'); }

                var statusBox = $(cell('STATUS'), '.v-box');
                setClass(statusBox, 'v-box', s.on ? 'v-ok' : 'v-na');
                setLastText(statusBox, s.text);

                setText($(cell('PRESSURE'), 'b'), ok(pre) ? num(pre.value, 0) : NA);

                // Tiêu chuẩn lấy từ tag VISCOSITY TC của PLC; chưa có dữ liệu (Kepware Unknown) thì hiện "--"
                var stdTag = map[key(m, sec, 3)];
                var hasStd = ok(stdTag);
                setText($(cell('TIÊU CHUẨN'), 'b'), hasStd ? num(stdTag.value, 1) : NA);
                setText($(cell('TIÊU CHUẨN'), 'small'), hasStd && act ? '(±' + num(act.tolerance, 1) + ')' : '');

                setClass($(cell('THỰC TẾ'), '.v-box'), 'v-box v-actual', 'is-' + (st === 'na' ? 'na' : st));
                setText($(cell('THỰC TẾ'), 'b'), ok(act) ? num(act.value, 1) : NA);

                var diff = ok(act) && act.standard !== null ? Math.round((act.value - act.standard) * 10) / 10 : null;
                setText($(cell('DIFF.'), 'b'), diff === null ? NA : (diff > 0 ? '+' : '') + num(diff, 1));

                var res = $(cell('RESULT'), '.v-box');
                setClass(res, 'v-box', st === 'ok' ? 'v-ok' : st === 'ng' ? 'v-ng' : 'v-na');
                setLastText(res, st === 'na' ? NA : st === 'ng' ? 'NG' : 'OK');
            });
        });
    }

    // ---------- Machine #01 ----------
    var MACHINE = +(document.body.dataset.machine || 1);
    function applyMach(map) {
        var cards = { c1: key(MACHINE, 'Stage1', 2), c2: key(MACHINE, 'Stage2', 2) };   // Temperature (c3): chưa có tag, để trống
        Object.keys(cards).forEach(function (c) {
            var card = $(document, '.m-card.' + c);
            if (!card) return;
            var d = map[cards[c]];
            var st = state(d);

            var stdTag = map[cards[c].replace(/\|2$/, '|3')];
            if (stdTag && ok(stdTag) && d && d.tolerance !== null)
                setFirstText($(card, '.m-std-val b'), num(stdTag.value, 1) + ' ± ' + num(d.tolerance, 1));
            else
                setFirstText($(card, '.m-std-val b'), NA);

            setClass($(card, '.m-actual'), 'm-actual', 'is-' + st);
            setText($(card, '.m-value b'), ok(d) ? num(d.value, 1) : NA);

            var r = $(card, '.m-result');
            setClass(r, 'm-result', 'is-' + st);
            setText(r, st === 'na' ? NA : st === 'ng' ? 'NG' : 'OK');
        });
    }

    var apply = page === 'ovw' ? applyOvw : page === 'visc' ? applyVisc : applyMach;

    // Số quá dài (ví dụ 16672,0) tự thu nhỏ để luôn nằm gọn trong ô
    var FIT = { ovw: ['.t-act-val', '.t-act'], visc: ['.v-val', '.v-box'], mach: ['.m-value', '.m-actual'] }[page];
    // Chưa có dữ liệu ("--") thì ẩn đơn vị đi kèm (s, MM, °C)
    function tidyUnits() {
        document.querySelectorAll('b + small').forEach(function (u) {
            var empty = u.previousElementSibling.textContent.trim().indexOf(NA) === 0;
            u.style.visibility = empty ? 'hidden' : '';
        });
        document.querySelectorAll('b > small').forEach(function (u) {   // đơn vị nằm trong thẻ b (ô TIÊU CHUẨN trang Machine)
            u.style.visibility = u.parentElement.textContent.trim().indexOf(NA) === 0 ? 'hidden' : '';
        });
    }

    function fitAll() {
        tidyUnits();
        document.querySelectorAll(FIT[0]).forEach(function (el) {
            var box = el.closest(FIT[1]);
            if (!box) return;
            el.style.transform = '';
            var avail = box.clientWidth - 12;
            var w = el.scrollWidth;
            el.style.transformOrigin = 'center';
            el.style.transform = w > avail && avail > 0 ? 'scale(' + (avail / w).toFixed(3) + ')' : '';
        });
    }
    window.addEventListener('resize', fitAll);

    function onData(list) {
        var map = {};
        list.forEach(function (d) { map[key(d.machineNo, d.section, d.metric)] = d; });
        apply(map);
        fitAll();
    }

    // Trước khi có dữ liệu (hoặc khi mất kết nối) hiện "--" thay vì số mẫu
    function showNoData() { apply({}); tidyUnits(); }
    showNoData();

    var es;
    function connect() {
        es = new EventSource('/api/tags/stream');
        es.onmessage = function (e) {
            try { onData(JSON.parse(e.data)); } catch (err) { /* bỏ qua gói lỗi */ }
        };
        es.onerror = function () { showNoData(); };   // EventSource tự kết nối lại
    }
    connect();
})();
