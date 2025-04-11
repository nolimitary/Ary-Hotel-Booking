document.addEventListener('DOMContentLoaded', () => {
    const picker = document.querySelector('.destination-picker');
    if (!picker) return;

    const searchInput = document.getElementById('destination-search-input');
    const suggestionsList = document.getElementById('suggestions-list');
    const applyBtn = document.getElementById('apply-destination-btn');

    let selectedDestination = null;

    const locations = [
        { name: "Paris, France", type: "city" }, { name: "London, UK", type: "city" },
        { name: "Rome, Italy", type: "city" }, { name: "Tokyo, Japan", type: "city" },
        { name: "New York, USA", type: "city" }, { name: "Barcelona, Spain", type: "city" },
        { name: "Dubai, UAE", type: "city" }, { name: "Singapore", type: "city" },
        { name: "Bangkok, Thailand", type: "city" }, { name: "Amsterdam, Netherlands", type: "city" },
        { name: "Istanbul, Turkey", type: "city" }, { name: "Los Angeles, USA", type: "city" },
        { name: "Prague, Czech Republic", type: "city" }, { name: "Vienna, Austria", type: "city" },
        { name: "Berlin, Germany", type: "city" }, { name: "Sydney, Australia", type: "city" },
        { name: "Sofia, Bulgaria", type: "city" }, { name: "Plovdiv, Bulgaria", type: "city" },
        { name: "Varna, Bulgaria", type: "city" }, { name: "Burgas, Bulgaria", type: "city" },
        { name: "France", type: "country" }, { name: "United Kingdom", type: "country" },
        { name: "Italy", type: "country" }, { name: "Japan", type: "country" },
        { name: "United States", type: "country" }, { name: "Spain", type: "country" },
        { name: "Bulgaria", type: "country" }
    ];

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
            if (item.dataset.value === value) {
                item.classList.add('is-selected');
            } else {
                item.classList.remove('is-selected');
            }
        });
    }


    searchInput.addEventListener('input', () => {
        const searchTerm = searchInput.value.trim().toLowerCase();
        selectedDestination = null; 
        applyBtn.disabled = true; 

        const allItems = suggestionsList.querySelectorAll('.destination-picker__suggestion-item');
        allItems.forEach(item => item.classList.remove('is-selected')); 

        if (searchTerm.length > 0) {
            const filteredLocations = locations.filter(location =>
                location.name.toLowerCase().includes(searchTerm)
            );
            renderSuggestions(filteredLocations);
        } else {
            renderSuggestions(locations); 
        }
    });

    suggestionsList.addEventListener('click', (event) => {
        const li = event.target.closest('.destination-picker__suggestion-item');
        if (li && li.dataset.value) {
            selectSuggestion(li.dataset.value);
        }
    });


    if (applyBtn) {
        applyBtn.addEventListener('click', () => {
            let finalDestination = selectedDestination || searchInput.value.trim();
            if (!finalDestination) return;

            const currentParams = new URLSearchParams(window.location.search);
            const existingDates = currentParams.get('selectedDates');
            const existingGuests = currentParams.get('selectedGuests');

            const newParams = new URLSearchParams();
            newParams.set('selectedLocation', finalDestination);
            if (existingDates) {
                newParams.set('selectedDates', existingDates);
            }
            if (existingGuests) {
                newParams.set('selectedGuests', existingGuests);
            }

            window.location.href = `/Home/Index?${newParams.toString()}`;
        });
    }

    renderSuggestions(locations); 
});