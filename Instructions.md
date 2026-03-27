## 📌 Setup Instructions
### Prerequisites
- [.NET SDK](https://dotnet.microsoft.com/download) (version 6.0 or later)
- A modern web browser with Web Speech API support (e.g., Chrome, Edge)
- SQLite (included with .NET SDK)


### Code Setup
```
# Clone using HTTPS
git clone https://github.com/Jaya30102003/Voice-Assistant-for-Blind.git
cd Voice-Assistant-for-Blind

dotnet clean 
dotnet restore 
dotnet build
```

### For Database Creation
```
dotnet tool install --global dotnet-ef
# If tool already installed
dotnet tool update --global dotnet-ef

# Proceed with the further commands only if build is success
dotnet ef migrations add InitialCreate
dotnet ef database update

dotnet run
```
## 📌 Admin Account

### To Seed Admin Credentials
```
http://localhost:5148/Admin/SeedAdmin
```

### to Login as Admin
```
http://localhost:5148/Admin/Login
```

## 🛠 Requirements

- .NET 9 SDK
- Windows OS (Tested on Windows 10/11)
