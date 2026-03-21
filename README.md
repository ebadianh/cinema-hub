# 🎬 CinemaHub

A modern fullstack cinema application for browsing movies, booking tickets, and managing users — built with a **React (Vite + TypeScript)** frontend and a **.NET backend**.

---

## 🚀 Features

- 🎥 Browse movies and showings
- 🎟️ Seat selection & booking system
- 🔐 Authentication & user roles (visitor / user / admin)
- 🧠 AI chat integration (cinema assistant)
- ⚙️ Admin panel (manage movies, showings, users)
- 📡 Real-time seat locking system
- 🎨 Modern UI with custom theme

---

## 🏗️ Tech Stack

### Frontend

- React (Vite)
- TypeScript
- Tailwind CSS
- Context API
- React Router

### Backend

- .NET (C#)
- REST API
- ACL (Access Control Layer)
- SQL Database

---

## 📁 Project Structure

```
cinema-hub-main/
│
├── backend/              # .NET backend
│   ├── src/              # API logic
│   ├── migrations/       # Database migrations
│   └── appsettings.json
│
├── src/                  # Frontend (React)
│   ├── components/
│   ├── pages/
│   ├── context/
│   ├── hooks/
│   └── utils/
│
├── public/               # Static assets
└── index.html
```

---

## ⚙️ Getting Started

### 1. Clone repo

```bash
git clone https://github.com/your-username/cinema-hub.git
cd cinema-hub
```

---

### 2. Run Application (Frontend + Backend)

```bash
npm install
npm run dev
```

App runs on:

```
http://localhost:5173
```

👉 Backend starts automatically via concurrent setup when running the dev script.

---

## 🔐 Authentication & Roles

| Role    | Permissions                     |
| ------- | ------------------------------- |
| Visitor | Browse movies                   |
| User    | Book tickets                    |
| Admin   | Full access (CRUD + management) |

ACL is enforced via backend rules.

---

## 🧠 AI Chat

CinemaHub includes an AI assistant that helps users:

- Find movies
- Get information about services
- Navigate the platform

---

## 🎟️ Booking System

- Real-time seat locking
- Prevents double booking
- Interactive seat selection UI

---

## 🧪 Future Improvements

- Payments integration (Stripe)
- Forgotten password
- Analytics for admin dashboard
- Further enhanced mobile optimization
- Performance optimizations (image loading)

---

## 📜 License

This project is licensed under the MIT License.

---

## 👤 Authors

Christian Meza
Havash Ebadian
Lukas Wennström
Mohammed Adam
Emmanuel Lowman

---

## 💡 Notes

This project was built as part of a fullstack development journey and focuses on:

- Solid architecture
- Real-world booking logic
- Scalable frontend structure
