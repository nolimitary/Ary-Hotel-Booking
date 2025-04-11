document.addEventListener('DOMContentLoaded', () => {
    const guestPicker = document.querySelector('.guest-picker');
    if (!guestPicker) {
        return;
    }

    const adultsCountSpan = document.getElementById('adults-count');
    const kidsCountSpan = document.getElementById('kids-count');
    const infantsCountSpan = document.getElementById('infants-count');
    const petsCheckbox = document.getElementById('pets-checkbox');
    const applyBtn = document.getElementById('apply-guests-btn');
    const controlRows = guestPicker.querySelectorAll('.guest-picker__controls');

    let counts = {
        adults: 1,
        kids: 0,
        infants: 0
    };

    const minValues = {
        adults: 1,
        kids: 0,
        infants: 0
    };

    const maxValues = {
        adults: 10,
        kids: 10,
        infants: 5
    };

    function updateDisplay() {
        adultsCountSpan.textContent = counts.adults;
        kidsCountSpan.textContent = counts.kids;
        infantsCountSpan.textContent = counts.infants;

        controlRows.forEach(row => {
            const type = row.dataset.type;
            const minusBtn = row.querySelector('[data-action="minus"]');
            const plusBtn = row.querySelector('[data-action="plus"]');
            if (minusBtn) {
                minusBtn.disabled = (counts[type] <= minValues[type]);
            }
            if (plusBtn) {
                plusBtn.disabled = (counts[type] >= maxValues[type]);
            }
        });
    }

    guestPicker.addEventListener('click', (event) => {
        const button = event.target.closest('.guest-picker__button');
        if (!button) return;

        const controls = button.closest('.guest-picker__controls');
        if (!controls) return;

        const action = button.dataset.action;
        const type = controls.dataset.type;

        if (action === 'plus' && counts[type] < maxValues[type]) {
            counts[type]++;
        } else if (action === 'minus' && counts[type] > minValues[type]) {
            counts[type]--;
        }

        updateDisplay();
    });

    if (applyBtn) {
        applyBtn.addEventListener('click', () => {
            let guestParts = [];
            if (counts.adults > 0) { guestParts.push(`${counts.adults} Adult${counts.adults > 1 ? 's' : ''}`); }
            if (counts.kids > 0) { guestParts.push(`${counts.kids} Kid${counts.kids > 1 ? 's' : ''}`); }
            if (counts.infants > 0) { guestParts.push(`${counts.infants} Infant${counts.infants > 1 ? 's' : ''}`); }
            if (petsCheckbox && petsCheckbox.checked) { guestParts.push('Pets'); }
            let newFormattedGuestString = guestParts.join(', ');
            if (!newFormattedGuestString) { newFormattedGuestString = '1 Adult'; }


            const currentParams = new URLSearchParams(window.location.search);
            const existingDates = currentParams.get('selectedDates');
            const existingLocation = currentParams.get('selectedLocation');

            const newParams = new URLSearchParams();
            newParams.set('selectedGuests', newFormattedGuestString); 
            if (existingDates) {
                newParams.set('selectedDates', existingDates); 
            }
            if (existingLocation) {
                newParams.set('selectedLocation', existingLocation);
            }

            window.location.href = `/Home/Index?${newParams.toString()}`;
        });
    }

    updateDisplay();
});