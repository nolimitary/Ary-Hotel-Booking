# 🏨 Aryaan's Hotel Booking

A modern, luxury-themed hotel booking site** built with **ASP.NET Core MVC**, styled to impress with dark elegance and custom UI features 💼🌙

---

## ✨ Features

🎨 **Dark luxury UI** – Stylish design inspired by premium hotel platforms  
🔎 **Search hotels by country or city**  
🌍 **Admin panel to add hotels** with image drag & drop  
🌟 **Star rating, review score, and pricing display**  
🗓️ **Full-page Litepicker calendar** (Skyscanner-style)  
📷 **Image uploads stored per hotel entry**  
🧾 **Review count & average score system**  
📱 **Responsive design** – Mobile-first and desktop perfect

---

## 🛠️ Built With

- `ASP.NET Core MVC`
- `Litepicker` (custom themed)  
- `Scoped CSS` with luxury dark theme  
- `JavaScript` for date logic + UI behavior  
- `Bootstrap` (customized)

---

## 🔧 AddHotel Panel

Admins can add new hotels via `/AddHotel`.  
Features include:

- Country & City input 🗺️  
- Star rating (1–5 stars) ⭐  
- Review count + average review score 📊  
- Price per night 💰  
- Image drag-and-drop uploader 🖼️  

Data instantly reflects on the home page hotel listings.

---

## 🧭 Folder Overview

├── wwwroot/
│   ├── images/                 # Uploaded hotel images
│   └── css/                    # Dark themed styles
├── Views/
│   ├── Home/                   # Landing page, search, results
│   ├── Admin/                  # AddHotel view
│   └── Shared/                 # Layout, partials
├── Models/                     # Hotel.cs etc.
├── Controllers/                # HomeController, AdminController
