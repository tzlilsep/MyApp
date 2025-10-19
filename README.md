# 🍳 TS – Smart Shopping & Recipe App (MAUI + AWS)

> **Status:** 🚧 *App in active development*  
> Built with **.NET MAUI**, **AWS Cognito**, and **DynamoDB**  
> Designed for **iPhone users** with a clean and friendly UI.

---

## 🛍️ Overview

**TS** is a personal project that combines two daily needs —  
a **Shopping List** and a **Cookbook** — into one smooth, connected experience.  
It’s designed for real daily use and is currently used by me and several friends.

Even though the app is still in progress, the core logic already supports:

- ✏️ Creating and editing shopping lists  
- ☁️ Cloud synchronization via AWS DynamoDB  
- 🔐 Secure login with AWS Cognito  
- 🧩 Modular architecture for easy feature expansion  

Next up is the **Cookbook module**, which will let users:
- Save and manage their favorite recipes  
- 🪄 Add recipe ingredients directly to a shopping list — instantly!

---

## 📱 Features (current & planned)

| Feature | Status |
|:--|:--:|
| User authentication (AWS Cognito) | ✅ |
| Shopping list management | ✅ |
| Recipe cookbook | 🔄 In development |
| Add recipe ingredients to list | 🔄 Planned |
| iPhone-friendly UI | ✅ |
| Cloud sync (DynamoDB) | ✅ |
| Multi-user support | 🔄 Planned |

---

## 🧠 Architecture

This project follows a **Clean Architecture** structure for scalability and clarity:
```
TS.sln
├── TS.Engine # Core domain and abstractions
│ ├── Abstractions/ # Interfaces for services (Auth, Lists)
│ ├── Contracts/ # DTOs (data transfer objects)
│ └── Domain/ # Core entities (User, ShoppingList, Item)
│
├── TS.AWS # AWS service implementations
│ ├── AwsAuthService.cs # Cognito login logic
│ ├── AwsShoppingListService.cs # DynamoDB data access
│ └── AwsClientsFactory.cs # AWS client setup
│
└── TS.UI # .NET MAUI cross-platform frontend
├── AppPages/StartPages/ # Login, Home
├── AppPages/ShoppingListApp/ # List view + editor
├── AppPages/CookBookApp/ # Coming soon 🍲
└── System/ # App setup & dependency injection

---

## 🧰 Tech Stack

- **.NET 8 + MAUI**
- **C#** for cross-platform logic  
- **AWS Cognito** for user authentication  
- **AWS DynamoDB** for cloud data storage  
- **MVVM architecture** with dependency injection  
- **XAML UI** with right-to-left (Hebrew) support

---

## 🧩 Why I Built It

This project started as a **personal tool** —  
I wanted a simple way to manage my recipes and shopping lists in one place.  
It grew into a **real daily-use app** for me and my friends.

I focused on:
- 🧠 **Clean logic** – easy to expand and maintain  
- 📲 **Practical usability** – designed for mobile-first experience  
- ☁️ **Cloud-first mindset** – full AWS integration

---

## 🚀 Roadmap

- [x] User login (Cognito)
- [x] Shopping list creation
- [ ] Cookbook screen + recipe storage
- [ ] Add ingredients directly to shopping list
- [ ] Publish on App Store (TestFlight stage)
- [ ] Add dark mode customization

---

## 👨‍💻 Author

**Tzlil Septon**  
Full Stack .NET Developer  
💡 Passionate about clean code, mobile apps, and cloud solutions.  
📫 [LinkedIn](https://www.linkedin.com/in/tzlil-septon-a8218725a/)

---

> 🍲 *This app is designed to make cooking and home management easier —  
> minimizing time and effort while enjoying delicious food.*
