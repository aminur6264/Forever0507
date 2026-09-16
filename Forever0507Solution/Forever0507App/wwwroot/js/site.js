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
