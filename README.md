# CardShop – Backend API

CardShop is a RESTful ASP.NET Core Web API that powers a full-stack trading card e-commerce application. The API handles authentication, product management, orders, and secure payment processing.

## Tech Stack
- C#
- ASP.NET Core Web API
- SQL Server
- ASP.NET Identity
- JWT Authentication
- Stripe API
- Cloudinary
- Entity Framework Core

## Key Features
- JWT-based authentication and role-based authorization
- User registration and login using ASP.NET Identity
- Product and inventory management
- Secure checkout and payment processing with Stripe
- Order creation and tracking
- Image upload and management via Cloudinary
- Clean separation of concerns using Controllers, Services, DTOs, and Repositories

## Architecture & Design
- RESTful API design following industry conventions
- DTOs used to decouple API contracts from database entities
- Service layer to encapsulate business logic
- Entity Framework Core for data access and migrations
- Designed to support future scalability and frontend clients

## Related Project
- **Frontend:** [CardShopApp](https://github.com/Justus78/CardShopApp)

