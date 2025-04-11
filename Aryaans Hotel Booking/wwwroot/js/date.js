document.addEventListener('DOMContentLoaded', () => {
    const calendarPicker = document.querySelector('.calendar-picker');
    if (!calendarPicker) {
        return;
    }

    const monthElements = calendarPicker.querySelectorAll('.calendar-month');
    const totalMonths = monthElements.length;
    let currentFirstVisibleMonthIndex = 0;
    const monthsToShow = 2;

    const applyBtn = document.getElementById('apply-dates-btn');
    let startDate = null;
    let endDate = null;

    function updateMonthVisibility() {
        monthElements.forEach(month => {
            month.classList.remove('is-visible');
        });
        for (let i = 0; i < monthsToShow; i++) {
            const monthIndexToShow = currentFirstVisibleMonthIndex + i;
            if (monthIndexToShow < totalMonths) {
                monthElements[monthIndexToShow].classList.add('is-visible');
            }
        }
        const allPrevButtons = calendarPicker.querySelectorAll('.calendar-month__nav--prev');
        const allNextButtons = calendarPicker.querySelectorAll('.calendar-month__nav--next');
        allPrevButtons.forEach(btn => btn.disabled = (currentFirstVisibleMonthIndex === 0));
        allNextButtons.forEach(btn => btn.disabled = (currentFirstVisibleMonthIndex >= totalMonths - monthsToShow));
    }

    function updateDayClasses() {
        const allDays = calendarPicker.querySelectorAll('.calendar-day:not(.is-placeholder)');
        allDays.forEach(day => {
            day.classList.remove('is-selected', 'is-start-date', 'is-end-date', 'is-in-range');
            let dayTime = null;
            try {
                const dateString = day.dataset.date;
                if (dateString) {
                    dayTime = new Date(dateString + 'T00:00:00Z').getTime();
                } else { return; }
            } catch (e) { console.error("Error parsing date: ", day.dataset.date, e); return; }

            if (startDate && endDate) {
                const startTime = startDate.getTime();
                const endTime = endDate.getTime();
                if (dayTime >= startTime && dayTime <= endTime) {
                    day.classList.add('is-in-range');
                    if (dayTime === startTime) { day.classList.add('is-selected', 'is-start-date'); }
                    if (dayTime === endTime) { day.classList.add('is-selected', 'is-end-date'); }
                }
            } else if (startDate && dayTime === startDate.getTime()) {
                day.classList.add('is-selected', 'is-start-date', 'is-end-date');
            }
        });
        if (applyBtn) {
            applyBtn.disabled = !startDate;
        }
    }

    calendarPicker.addEventListener('click', (event) => {
        const prevButton = event.target.closest('.calendar-month__nav--prev');
        const nextButton = event.target.closest('.calendar-month__nav--next');

        if (prevButton && !prevButton.disabled) {
            if (currentFirstVisibleMonthIndex > 0) {
                currentFirstVisibleMonthIndex--;
                updateMonthVisibility();
            }
            return;
        }

        if (nextButton && !nextButton.disabled) {
            if (currentFirstVisibleMonthIndex < totalMonths - monthsToShow) {
                currentFirstVisibleMonthIndex++;
                updateMonthVisibility();
            }
            return;
        }

        const targetDay = event.target.closest('.calendar-day:not(:disabled):not(.is-placeholder)');
        if (targetDay) {
            const selectedDate = new Date(targetDay.dataset.date + 'T00:00:00Z');
            if (!startDate || (startDate && endDate)) {
                startDate = selectedDate;
                endDate = null;
            } else if (selectedDate.getTime() === startDate.getTime()) {
                startDate = null;
                endDate = null;
            } else if (selectedDate < startDate) {
                startDate = selectedDate;
                endDate = null;
            } else {
                endDate = selectedDate;
            }
            updateDayClasses();
        }
    });

    if (applyBtn) {
        applyBtn.addEventListener('click', () => {
            if (!startDate) return;

            const formatDateForDisplay = (date) => {
                const localDate = new Date(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate());
                return localDate.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
            };

            let newFormattedDateString = formatDateForDisplay(startDate);
            if (endDate && endDate.getTime() !== startDate.getTime()) {
                newFormattedDateString += ` - ${formatDateForDisplay(endDate)}`;
            }

            const currentParams = new URLSearchParams(window.location.search);
            const existingGuests = currentParams.get('selectedGuests');
            const existingLocation = currentParams.get('selectedLocation');

            const newParams = new URLSearchParams();
            newParams.set('selectedDates', newFormattedDateString);
            if (existingGuests) {
                newParams.set('selectedGuests', existingGuests);
            }
            if (existingLocation) {
                newParams.set('selectedLocation', existingLocation);
            }

          
            window.location.href = `/Home/Index?${newParams.toString()}`;
        });
    }

    updateMonthVisibility();
    updateDayClasses();

});