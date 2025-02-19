# PtixiakiReservations

This is a .NET 8.0 ASP.NET Core application that serves as a reservation management system. The application uses SQL Server for its database and provides a ready-to-go environment for hosting.

## Prerequisites

### General Requirements
- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/) instance for the database
- [Docker](https://www.docker.com/) (if running the application in a container)

### Configuration

You need to configure the connection string to point to your SQL Server database. Edit the `appsettings.json` file in the project directory (or use environment variables to override it in Docker). Example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=<YourServer>;Database=<YourDatabase>;User Id=<YourUser>;Password=<YourPassword>;"
  }
}
```

---

## How to Run the Application

### Running Without Docker

1. **Restore Dependencies**  
   Run the following command to restore the .NET dependencies:
   ```bash
   dotnet restore
   ```

2. **Apply Database Migrations**  
   To set up or update the database with the latest changes:
   ```bash
   dotnet ef database update
   ```
   Ensure the `appsettings.json` file is correctly configured with your SQL Server connection.

3. **Run the Application**  
   To start the application:
   ```bash
   dotnet run --project PtixiakiReservations/PtixiakiReservations.csproj
   ```

4. Open your browser and navigate to:
   ```
   https://localhost:5001
   ```

---

### Running with Docker

1. **Build the Docker Image**  
   In the project root directory (where the Dockerfile is located), run:
   ```bash
   docker build -t ptixiaki-reservations .
   ```

2. **Run the Container**  
   After the image is built, run the container:
   ```bash
   docker run -d -p 8080:8080 -p 8081:8081 --env ConnectionStrings__DefaultConnection="Server=<YourServer>;Database=<YourDatabase>;User Id=<YourUser>;Password=<YourPassword>;" ptixiaki-reservations
   ```

   Replace `<YourServer>`, `<YourDatabase>`, `<YourUser>`, and `<YourPassword>` with the actual database connection details.

3. Open your browser and navigate to:
   ```
   http://localhost:8080
   ```

---

## Additional Information

### Database Seeding

The application automatically seeds default data into the database. This process is executed when the application starts for the first time.

### Environment Settings

- Development Mode: Includes debugging tools like the Developer Exception Page and auto-migrations.
- Production Mode: Enforces HTTPS and includes comprehensive error handling.

### Ports Used by the Application

- **8080**: Primary HTTP Port
- **8081**: Secondary Port

---

## Troubleshooting

- Ensure that the database is correctly set up and accessible from the application.
- For Dockerized setups, verify that the `ConnectionStrings__DefaultConnection` environment variable is provided correctly.
- If issues persist, check logs from the application:
  ```bash
  docker logs <container_id>
  ```

---

## Contributing

Feel free to submit issues or contribute to this project by creating pull requests.

---

## License

This project is licensed under [YOUR LICENSE]. Update this section with your license file details.