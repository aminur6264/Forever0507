// Live payable-amount estimate (main + 2%) and the "other school" toggle.
// Mirrors Services.FeeCalculator — the server value is authoritative.
(function () {
    'use strict';

    var BN = '০১২৩৪৫৬৭৮৯';
    function bn(value) {
        return String(value).replace(/\d/g, function (d) { return BN[+d]; });
    }
    function taka(amount) {
        return '৳' + bn(amount.toLocaleString('en-IN'));
    }

    // ---- payable amount = main + 2% (rounded up) ----
    var amount = document.getElementById('AmountText');
    var payablePreview = document.getElementById('payablePreview');
    var feeEstimate = document.getElementById('feeEstimate');

    function updatePayable() {
        var raw = (amount && amount.value ? amount.value : '').trim();
        var value = parseFloat(raw);
        if (!/^\d+(\.\d{1,2})?$/.test(raw) || isNaN(value) || value < 1000) {
            if (payablePreview) payablePreview.textContent = '—';
            if (feeEstimate) feeEstimate.textContent = '—';
            return;
        }
        var payable = Math.ceil(value * 1.02);
        if (payablePreview) payablePreview.textContent = taka(payable);
        if (feeEstimate) feeEstimate.textContent = taka(payable);
    }

    if (amount) {
        amount.addEventListener('input', updatePayable);
        amount.addEventListener('change', updatePayable);
        updatePayable();
    }

    // ---- school dropdown: reveal a text box for "other" ----
    var OTHER = '__OTHER__';
    var schoolChoice = document.getElementById('SchoolChoice');
    var otherWrap = document.getElementById('otherSchoolWrap');
    var otherName = document.getElementById('OtherSchoolName');

    function toggleOther() {
        if (!schoolChoice || !otherWrap) return;
        var isOther = schoolChoice.value === OTHER;
        otherWrap.classList.toggle('d-none', !isOther);
        if (otherName) otherName.disabled = !isOther;
        if (isOther && otherName) otherName.focus();
    }

    if (schoolChoice) {
        schoolChoice.addEventListener('change', toggleOther);
        toggleOther();
    }
})();
