// Global site JS

$(document).ready(function () {
    // Confirm delete before form submit
    $(document).on('submit', '.confirm-delete', function (e) {
        if (!confirm('Are you sure you want to delete this? This action cannot be undone.')) {
            e.preventDefault();
        }
    });

    // Auto-dismiss alerts after 4 seconds
    setTimeout(function () {
        $('.alert-dismissible').fadeOut('slow');
    }, 4000);
});
