// Nhận dữ liệu realtime qua Server-Sent Events (/api/tags/stream) và cập nhật các trang Overview, Viscosity, Machine
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
    function shortNum(v) {
        if (v === null || v === undefined || isNaN(v)) return NA;
        return (v % 1 === 0 ? String(v) : Number(v).toFixed(1)).replace('.', ',');
    }
    function $(root, sel) { return root ? root.querySelector(sel) : null; }
    function setText(el, text) { if (el) el.textContent = text; }
    function setLastText(el, text) {
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

    function ok(d) { return d && d.value !== null && d.value !== undefined; }
    function state(d) {
        if (!ok(d) || !d.result) return 'na';
        return d.result === 'NG' ? 'ng' : 'ok';
    }
    // Nhiệt độ hiển thị số nguyên: OK/NG tính theo đúng số đang hiển thị (đã làm tròn)
    function tempState(d) {
        if (!ok(d)) return 'na';
        if (d.standard === null || d.standard === undefined || d.tolerance === null || d.tolerance === undefined) return state(d);
        return Math.abs(Math.round(d.value) - d.standard) <= d.tolerance + 1e-9 ? 'ok' : 'ng';
    }
    function statusInfo(d) {
        if (!ok(d)) return { text: 'OFFLINE', on: false };
        return d.value >= 1 ? { text: 'START', on: true } : { text: 'STOP', on: false };
    }

    // Metalcore HMI Overview
    function applyOvw(map) {
        document.querySelectorAll('.t-card').forEach(function (card) {
            var m = +card.dataset.machine;
            var t = map[key(m, 'Temperature', 2)];
            var st = tempState(t);

            var s = statusInfo(map[key(m, 'Machine', 0)]);
            var start = $(card, '.t-start');
            setClass(start, 't-start', s.on ? '' : 'is-off');
            setLastText(start, s.text);

            if (t && t.standard !== null && t.tolerance !== null) {
                setFirstText($(card, '.t-std-n'), shortNum(t.standard));
                setText($(card, '.t-std-n small'), t.unit || '°C');
                setText($(card, '.t-std-tol'), '± ' + shortNum(t.tolerance));
            }

            setClass($(card, '.t-act'), 't-act', 't-' + st);
            setText($(card, '.t-act-val b'), ok(t) ? num(t.value, 0) : NA);

            var pill = $(card, '.t-pill');
            setClass(pill, 't-pill', 't-' + st);
            setLastText(pill, st === 'na' ? NA : (st === 'ng' ? 'NG' : 'OK'));
            var use = pill && pill.querySelector('use');
            if (use) use.setAttribute('href', st === 'ng' ? '#i-cross' : '#i-check');
            card.classList.toggle('is-ng', st === 'ng');
        });
    }

    // Viscosity
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

                var stdTag = map[key(m, sec, 3)];
                var hasStd = ok(stdTag);
                setText($(cell('TIÊU CHUẨN'), 'b'), hasStd ? num(stdTag.value, 1) : NA);
                setText($(cell('TIÊU CHUẨN'), 'small'), hasStd && act ? '(±' + num(act.tolerance, 1) + ')' : '');

                setClass($(cell('THỰC TẾ'), '.v-box'), 'v-box v-actual', 'is-' + (st === 'na' ? 'na' : st));
                setText($(cell('THỰC TẾ'), 'b'), ok(act) ? num(act.value, 1) : NA);

                var diff = ok(act) && hasStd ? Math.round((act.value - stdTag.value) * 10) / 10 : null;
                setText($(cell('DIFF.'), 'b'), diff === null ? NA : (diff > 0 ? '+' : '') + num(diff, 1));

                var res = $(cell('KẾT QUẢ'), '.v-box');
                setClass(res, 'v-box', st === 'ok' ? 'v-ok' : st === 'ng' ? 'v-ng' : 'v-na');
                setLastText(res, st === 'na' ? NA : st === 'ng' ? 'NG' : 'OK');
            });
        });
    }

    // Machine
    var MACHINE = +(document.body.dataset.machine || 1);
    function applyMach(map) {
        var cards = {
            c1: key(MACHINE, 'Stage1', 2),
            c2: key(MACHINE, 'Stage2', 2),
            c3: key(MACHINE, 'Temperature', 2)
        };
        Object.keys(cards).forEach(function (c) {
            var card = $(document, '.m-card.' + c);
            if (!card) return;
            var d = map[cards[c]];
            var dec = c === 'c3' ? 0 : 1;
            var st = c === 'c3' ? tempState(d) : state(d);

            // Độ nhớt: chuẩn lấy từ tag VISCOSITY TC của PLC. Nhiệt độ: chuẩn cấu hình (70 ± 10).
            var stdValue = null;
            if (c === 'c3') {
                if (d && d.standard !== null) stdValue = d.standard;
            } else {
                var stdTag = map[cards[c].replace(/\|2$/, '|3')];
                if (ok(stdTag)) stdValue = stdTag.value;
            }
            if (stdValue !== null && d && d.tolerance !== null)
                setFirstText($(card, '.m-std-val b'), num(stdValue, dec) + ' ± ' + num(d.tolerance, dec));
            else
                setFirstText($(card, '.m-std-val b'), NA);

            setClass($(card, '.m-actual'), 'm-actual', 'is-' + st);
            setText($(card, '.m-value b'), ok(d) ? num(d.value, dec) : NA);

            var r = $(card, '.m-result');
            setClass(r, 'm-result', 'is-' + st);
            setText(r, st === 'na' ? NA : st === 'ng' ? 'NG' : 'OK');
        });
    }

    var apply = page === 'ovw' ? applyOvw : page === 'visc' ? applyVisc : applyMach;

    // Số quá dài tự thu nhỏ để nằm gọn trong ô
    var FIT = { ovw: ['.t-act-val', '.t-act'], visc: ['.v-val', '.v-box'], mach: ['.m-value', '.m-actual'] }[page];
    // Ẩn đơn vị (s, PPM, °C) khi giá trị là "--"
    function tidyUnits() {
        document.querySelectorAll('b + small').forEach(function (u) {
            var empty = u.previousElementSibling.textContent.trim().indexOf(NA) === 0;
            u.style.visibility = empty ? 'hidden' : '';
        });
        document.querySelectorAll('b > small').forEach(function (u) {
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

    function showNoData() { apply({}); tidyUnits(); }
    showNoData();

    var es;
    function connect() {
        es = new EventSource('/api/tags/stream');
        es.onmessage = function (e) {
            try { onData(JSON.parse(e.data)); } catch (err) { /* bỏ qua gói lỗi */ }
        };
        es.onerror = function () { showNoData(); };
    }
    connect();
})();
