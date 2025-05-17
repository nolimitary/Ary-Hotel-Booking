# Aryaan's Hotel Booking

Welcome to Aryaan's Hotel Booking, a web application designed to provide a seamless hotel browsing and booking experience. This project is currently under development, evolving from a foundational hotel listing site into a more comprehensive booking platform.

## 1. Project Overview

Aryaan's Hotel Booking aims to be a user-friendly platform for discovering, viewing, and eventually booking hotel accommodations. The project started as a demonstration of a custom-styled ASP.NET Core MVC application with features like hotel search, a custom calendar, and an admin panel for adding hotel listings. The goal is to expand this into a fully functional booking system with user accounts, dynamic room management, and a robust booking process.

**Key Goals:**
* Provide an intuitive interface for users to search and find hotels.
* Implement a complete booking and reservation management system.
* Allow administrators to manage hotel listings, room types, and bookings.
* Enable users to register, manage their profiles, and view their booking history.
* Incorporate a review and rating system for hotels.

## 2. Current Features (as of initial state)

* **Hotel Listings:** Displays hotels with details such as name, star rating, (static) review score, and price.
* **Search Functionality:** Users can search for hotels by country or city.
* **Custom Theming:** Features a "Luxury Dark Theme" with scoped CSS for a unique look and feel.
* **Date Picker:** Utilizes Litepicker, a JavaScript date range picker, styled to match the dark theme.
* **Guest Picker:** A UI component for selecting the number of adults and children.
* **Destination Picker:** A UI component for selecting the destination.
* **Admin Panel (`/AddHotel`):**
    * Allows adding new hotels with details like name, country, city, price, star rating, and review score.
    * Supports drag-and-drop image uploading for hotel pictures.
    * Newly added hotels and their images are stored locally (`wwwroot/hotels.txt` and `wwwroot/images/`) and reflected on the home page.
* **Responsive Design:** Basic responsiveness for different screen sizes.

## 3. Planned Features & Roadmap

The project will be enhanced with the following key features, progressively developed:

1.  **Database Integration:**
    * Transition from `hotels.txt` to a relational database (SQLite with Entity Framework Core initially).
    * Define entities for Hotels, Rooms, Bookings, Users, and Reviews.
2.  **User Authentication & Authorization:**
    * Implement user registration and login using ASP.NET Core Identity.
    * Differentiate between user roles (e.g., Customer, Administrator).
3.  **Room Management (Admin):**
    * Allow admins to define and manage various room types for each hotel (e.g., Standard, Deluxe, Suite) including details like capacity, amenities, price, and availability.
4.  **Core Booking System:**
    * Enable users to select rooms and book them for specific dates.
    * Implement room availability checks.
    * Provide booking confirmation and allow users to view their booking history.
    * Allow admins to view and manage all bookings.
5.  **Dynamic Reviews and Ratings:**
    * Allow authenticated users to submit reviews and ratings for hotels.
    * Display average ratings and individual reviews on hotel detail pages.
6.  **Advanced Search & Filtering:**
    * Enhance search with filters for price range, amenities, star ratings, etc.
    * Implement sorting options for search results.
7.  **Payment Processing (Placeholder):**
    * Integrate a basic placeholder for the payment step in the booking process.
8.  **Enhanced Error Handling & Validation:**
    * Implement comprehensive server-side and client-side validation.
9.  **Notifications:**
    * Basic email notifications for events like booking confirmation (placeholder/mock initially).

## 4. Technology Stack

* **Backend:** ASP.NET Core MVC (.NET 7 or newer)
* **Language:** C#
* **Database:** Initially SQLite with Entity Framework Core (EF Core)
* **Frontend:**
    * HTML5, CSS3, JavaScript
    * Bootstrap (customized)
    * jQuery
* **Key Libraries/Tools:**
    * Litepicker (for date range selection)
    * ASP.NET Core Identity (for authentication)
    * Serilog (for logging - planned)

## 5. Getting Started

### Prerequisites

* [.NET SDK](https://dotnet.microsoft.com/download) (Version 7.0 or later recommended)
* A code editor or IDE like Visual Studio, VS Code, or JetBrains Rider.

### Installation & Setup

1.  **Clone the repository:**
    ```bash
    git clone (https://github.com/nolimitary/Ary-Hotel-Booking)
    ```
2.  **Navigate to the project directory:**
    ```bash
    cd "Aryaans Hotel Booking/Aryaans Hotel Booking"
    ```
    (Or the appropriate path to the `.csproj` file)
3.  **Restore .NET dependencies:**
    ```bash
    dotnet restore
    ```

### Running the Application

1.  **Run the application from the project directory:**
    ```bash
    dotnet run
    ```
2.  **Access the application:**
    Open your web browser and navigate to `https://localhost:XXXX` or `http://localhost:YYYY`, where XXXX/YYYY are the port numbers specified in `Properties/launchSettings.json` (e.g., `https://localhost:7075`).

## 6. Project Structure Highlights

* **`/Controllers`**: Contains the C# controller classes that handle incoming HTTP requests.
* **`/Models`**: Contains C# model classes, including ViewModels for passing data to views and (eventually) Entities for EF Core.
* **`/Views`**: Contains Razor (.cshtml) files for rendering the UI.
    * **`/Views/Shared`**: Common layout files and partial views.
* **`/wwwroot`**: Static assets like CSS, JavaScript, images, and third-party libraries.
    * **`/wwwroot/css`**: Custom CSS files for styling.
    * **`/wwwroot/js`**: Custom JavaScript files.
    * **`/wwwroot/images`**: Stores uploaded hotel images.
    * **`/wwwroot/hotels.txt`**: (Legacy) Text file used for storing hotel data in the initial version.
* **`Program.cs`**: Main entry point of the application, configures services and middleware.
* **`appsettings.json`**: Configuration file for the application.

## 7. Admin Panel (Current Version)

The current version includes a basic admin panel to add new hotels:
* **URL:** `/AddHotel`
* **Functionality:** Allows adding hotel details and uploading images. Data is saved to `wwwroot/hotels.txt` and images to `wwwroot/images/`.

---

This README will be updated as the project progresses.
