(function ($) {
    if (!$ || !$.validator) return;
    $(function () {
        const date = document.getElementById('Input_InstalledOn');
        if (!date || !date.form) return;
        const validator = $(date.form).data('validator');
        if (!validator) return;
        // This bundled validator's numeric step rule throws for datetime-local.
        // Date binding and server/database date checks remain authoritative.
        const previousIgnore = validator.settings.ignore;
        validator.settings.ignore = function (index, element) {
            if (element === date) return true;
            return typeof previousIgnore === 'function'
                ? previousIgnore.call(this, index, element)
                : $(element).is(previousIgnore || ':hidden');
        };
    });
})(window.jQuery);
