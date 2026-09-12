// Live participation-fee estimate. Mirrors Services.FeeCalculator — the server value is authoritative.
(function () {
    'use strict';

    var out = document.getElementById('feeEstimate');
    if (!out) return;

    var batch = document.getElementById('BatchYear');
    var extra = document.getElementById('ExtraMembers');
    var children = document.getElementById('ChildrenUnder5');

    var BN = '০১২৩৪৫৬৭৮৯';
    function bn(value) {
        return String(value).replace(/\d/g, function (d) { return BN[+d]; });
    }
    function taka(amount) {
        return '৳' + bn(amount.toLocaleString('en-IN'));
    }
    function num(el) {
        var n = parseInt(el && el.value, 10);
        return isNaN(n) || n < 0 ? 0 : n;
    }

    function update() {
        var year = num(batch);
        if (year < 1960 || year > 2026) {
            out.textContent = '—';
            return;
        }
        var extraMembers = Math.min(num(extra), 5);
        var fee = (year <= 2019 ? 1000 : 500) + extraMembers * 500;
        out.textContent = taka(fee);
    }

    ['input', 'change'].forEach(function (evt) {
        [batch, extra, children].forEach(function (el) {
            if (el) el.addEventListener(evt, update);
        });
    });

    update();
})();
