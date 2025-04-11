function toggleDatePicker() {
    const picker = document.getElementById('customDatePicker');
    picker.classList.toggle('show');
}

function applyDates() {
    const checkIn = document.getElementById('checkIn').value;
    const checkOut = document.getElementById('checkOut').value;

    const dateInput = document.getElementById('dateRangePicker');
    if (checkIn && checkOut) {
        dateInput.value = `${checkIn} to ${checkOut}`;
        toggleDatePicker();
    }
}

document.addEventListener('DOMContentLoaded', () => {
    const input = document.getElementById('dateRangePicker');
    if (input) {
        input.addEventListener('click', function (e) {
            e.preventDefault();
            toggleDatePicker();
        });
    }
});


