// Countdown timer for the reunion start time (rendered server-side via data-start).
(function () {
    'use strict';

    var el = document.getElementById('countdown');
    if (!el) return;

    var target = new Date(el.dataset.start).getTime();
    if (isNaN(target)) {
        el.remove();
        return;
    }

    var BN = '০১২৩৪৫৬৭৮৯';
    function bn(value) {
        return String(value).replace(/\d/g, function (d) { return BN[+d]; });
    }
    function pad(n) { return n < 10 ? '0' + n : String(n); }

    function tick() {
        var diff = target - Date.now();
        if (diff <= 0) {
            el.innerHTML = '<div class="cd__done">অনুষ্ঠান শুরু হয়ে গেছে — সবাই আসবেন!</div>';
            clearInterval(timer);
            return;
        }
        var s = Math.floor(diff / 1000);
        el.querySelector('[data-cd="d"]').textContent = bn(Math.floor(s / 86400));
        el.querySelector('[data-cd="h"]').textContent = bn(pad(Math.floor((s % 86400) / 3600)));
        el.querySelector('[data-cd="m"]').textContent = bn(pad(Math.floor((s % 3600) / 60)));
        el.querySelector('[data-cd="s"]').textContent = bn(pad(s % 60));
    }

    tick();
    var timer = setInterval(tick, 1000);
})();
