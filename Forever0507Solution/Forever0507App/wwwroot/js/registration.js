// "Other school" toggle and the transaction-medium → committee-account dropdown.
(function () {
    'use strict';

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
