// Home-page countdown — ticks down to the hero's data-start ISO instant (Asia/Dhaka offset
// baked in by EventOptions.StartTimeIso). Shows ০০ everywhere once the event has begun.
(function () {
    'use strict';

    var BN = '০১২৩৪৫৬৭৮৯';
    function bn(value) {
        return String(value).replace(/\d/g, function (d) { return BN[+d]; });
    }
    function pad(value) {
        return value < 10 ? '0' + value : String(value);
    }

    var box = document.getElementById('countdown');
    if (!box) return;

    var start = new Date(box.dataset.start).getTime();
    if (isNaN(start)) return;

    var cells = {
        d: box.querySelector('[data-cd="d"]'),
        h: box.querySelector('[data-cd="h"]'),
        m: box.querySelector('[data-cd="m"]'),
        s: box.querySelector('[data-cd="s"]'),
    };
    if (!cells.d || !cells.h || !cells.m || !cells.s) return;

    function render() {
        var left = Math.max(0, start - Date.now());
        cells.d.textContent = bn(Math.floor(left / 86400000));
        cells.h.textContent = bn(pad(Math.floor(left % 86400000 / 3600000)));
        cells.m.textContent = bn(pad(Math.floor(left % 3600000 / 60000)));
        cells.s.textContent = bn(pad(Math.floor(left % 60000 / 1000)));
    }

    render();
    setInterval(render, 1000);
})();

// Gallery lightbox — clicking a home-page gallery shot opens it full-width; arrow buttons
// (and ←/→ keys) step through the shots, Esc or a backdrop click closes.
(function () {
    'use strict';

    var box = document.getElementById('galleryLightbox');
    var shots = Array.prototype.slice.call(document.querySelectorAll('.gallery-shot'));
    if (!box || shots.length === 0) return;

    var BN = '০১২৩৪৫৬৭৮৯';
    var img = box.querySelector('.lightbox__img');
    var caption = box.querySelector('.lightbox__caption');
    var count = box.querySelector('.lightbox__count');
    var prev = box.querySelector('[data-lightbox-prev]');
    var next = box.querySelector('[data-lightbox-next]');
    var current = 0;

    // One picture in the gallery needs no stepping UI.
    if (shots.length < 2) {
        prev.style.display = 'none';
        next.style.display = 'none';
        count.style.display = 'none';
    }

    function show(index) {
        current = (index + shots.length) % shots.length;
        var shot = shots[current];
        img.src = shot.dataset.full;
        img.alt = shot.dataset.caption;
        caption.textContent = shot.dataset.caption;
        count.textContent = bn(current + 1) + ' / ' + bn(shots.length);
    }

    function bn(value) {
        return String(value).replace(/\d/g, function (d) { return BN[+d]; });
    }

    function open(index) {
        show(index);
        box.hidden = false;
        document.body.style.overflow = 'hidden';
        box.querySelector('[data-lightbox-close]').focus();
    }

    function close() {
        box.hidden = true;
        img.src = '';
        document.body.style.overflow = '';
    }

    shots.forEach(function (shot, index) {
        shot.addEventListener('click', function () { open(index); });
    });

    prev.addEventListener('click', function () { show(current - 1); });
    next.addEventListener('click', function () { show(current + 1); });
    box.querySelector('[data-lightbox-close]').addEventListener('click', close);

    // Clicking the dark backdrop (not the picture or a control) closes.
    box.addEventListener('click', function (e) {
        if (e.target === box) close();
    });

    document.addEventListener('keydown', function (e) {
        if (box.hidden) return;
        if (e.key === 'Escape') close();
        else if (e.key === 'ArrowLeft' && shots.length > 1) show(current - 1);
        else if (e.key === 'ArrowRight' && shots.length > 1) show(current + 1);
    });
})();
