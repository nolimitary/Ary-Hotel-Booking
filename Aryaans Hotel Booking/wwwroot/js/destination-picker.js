document.addEventListener('DOMContentLoaded', () => {
    const picker = document.querySelector('.destination-picker');
    if (!picker) return;

    const searchInput = document.getElementById('destination-search-input');
    const suggestionsList = document.getElementById('suggestions-list');
    const applyBtn = document.getElementById('apply-destination-btn');

    let selectedDestination = null;

    // 🌍 
    const locations = [
        "Australia", "Austria", "Belgium", "Brazil", "Bulgaria", "Canada", "China", "Croatia",
        "Czech Republic", "Denmark", "Egypt", "Finland", "France", "Germany", "Greece", "Hungary",
        "India", "Indonesia", "Ireland", "Israel", "Italy", "Japan", "Malaysia", "Mexico",
        "Netherlands", "New Zealand", "Norway", "Philippines", "Poland", "Portugal", "Romania",
        "Russia", "Saudi Arabia", "Singapore", "Slovakia", "Slovenia", "South Africa", "South Korea",
        "Spain", "Sweden", "Switzerland", "Thailand", "Turkey", "Ukraine", "United Arab Emirates",
        "United Kingdom", "United States", "Vietnam"
    ].map(name => ({ name, type: "country" }));

    function renderSuggestions(items) {
        suggestionsList.innerHTML = '';
        items.forEach(item => {
            const li = document.createElement('li');
            li.classList.add('destination-picker__suggestion-item');
            li.textContent = item.name;
            li.dataset.value = item.name;
            suggestionsList.appendChild(li);
        });
    }

    function selectSuggestion(value) {
        selectedDestination = value;
        searchInput.value = value;
        applyBtn.disabled = false;

        const allItems = suggestionsList.querySelectorAll('.destination-picker__suggestion-item');
        allItems.forEach(item => {
            item.classList.toggle('is-selected', item.dataset.value === value);
        });
    }

    searchInput.addEventListener('input', () => {
        const searchTerm = searchInput.value.trim().toLowerCase();
        selectedDestination = null;
        applyBtn.disabled = true;

        const allItems = suggestionsList.querySelectorAll('.destination-picker__suggestion-item');
        allItems.forEach(item => item.classList.remove('is-selected'));

        const filteredLocations = searchTerm
            ? locations.filter(location => location.name.toLowerCase().includes(searchTerm))
            : locations;

        renderSuggestions(filteredLocations);
    });

    suggestionsList.addEventListener('click', (event) => {
        const li = event.target.closest('.destination-picker__suggestion-item');
        if (li && li.dataset.value) {
            selectSuggestion(li.dataset.value);
        }
    });

    if (applyBtn) {
        applyBtn.addEventListener('click', () => {
            const finalDestination = selectedDestination || searchInput.value.trim();
            if (!finalDestination) return;

            const currentParams = new URLSearchParams(window.location.search);
            const existingDates = currentParams.get('selectedDates');
            const existingGuests = currentParams.get('selectedGuests');

            const newParams = new URLSearchParams();
            newParams.set('selectedLocation', finalDestination);
            if (existingDates) newParams.set('selectedDates', existingDates);
            if (existingGuests) newParams.set('selectedGuests', existingGuests);

            window.location.href = `/Home/Index?${newParams.toString()}`;
        });
    }

    renderSuggestions(locations);
});
