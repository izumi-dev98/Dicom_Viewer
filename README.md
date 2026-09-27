# DICOM Viewer - ASP.NET Core Web API

A lightweight ASP.NET Core 8.0 Web API for managing DICOM medical imaging files. This application allows uploading, storing, and retrieving DICOM study metadata using fo-dicom library and SQL Server.

---

## 📋 Table of Contents

- [Project Overview](#project-overview)
- [Project Structure](#project-structure)
- [Architecture & Flow](#architecture--flow)
- [API Endpoints](#api-endpoints)
- [Database Schema](#database-schema)
- [Example DICOM File](#example-dicom-file)
- [Prerequisites](#prerequisites)
- [Setup & Installation](#setup--installation)
- [Configuration](#configuration)
- [Running the Application](#running-the-application)
- [Testing with Swagger](#testing-with-swagger)
- [Dependencies](#dependencies)

---

## 🎯 Project Overview

The **DICOM Viewer** is a RESTful API built with ASP.NET Core 8.0 that provides:

- **DICOM File Upload** - Accept `.dcm` files via multipart/form-data
- **Metadata Extraction** - Automatically parses DICOM tags (Patient Name, ID, Study UID, Modality, Study Date)
- **Database Storage** - Persists study metadata to SQL Server using Dapper
- **File Storage** - Saves physical DICOM files to local storage
- **CRUD Operations** - Full Create, Read, Delete for DICOM studies
- **Swagger/OpenAPI** - Built-in API documentation and testing interface

---

## 📁 Project Structure

```
Dicom_Viewer/
├── Dicom Viewer.slnx                    # Solution file
├── README.md                            # This file
├── .gitignore                           # Git ignore rules (excludes large DICOM files)
└── Dicom Viewer/                        # Main project folder
    ├── Dicom Viewer.csproj              # Project configuration
    ├── Program.cs                       # Application entry point & DI configuration
    ├── appsettings.json                 # Production configuration
    ├── appsettings.Development.json     # Development configuration
    ├── Dicom Viewer.http                # HTTP request examples for testing
    │
    ├── Controllers/
    │   └── DicomController.cs           # REST API endpoints
    │
    ├── Models/
    │   └── DicomStudyModels.cs          # DICOM study data model
    │
    ├── Repositories/
    │   └── DicomViewerRepo.cs           # Data access layer (Dapper + SQL Server)
    │
    ├── Services/
    │   └── DicomHapper.cs               # DICOM processing service (fo-dicom)
    │
    ├── Properties/
    │   └── launchSettings.json          # Launch profiles (IIS Express, Kestrel)
    │
    └── Storage/
        └── dicom_files/                 # Physical DICOM file storage (empty by default)
            # Add your own .dcm files here for testing
            # Sample source: Siemens Healthineers Magnetom World DICOM Images
```

### Key Components

| Component | Responsibility |
|-----------|----------------|
| **DicomController** | HTTP endpoints, request validation, file upload handling |
| **DicomHapper (Service)** | DICOM file parsing, metadata extraction using fo-dicom |
| **DicomViewerRepo (Repository)** | Database CRUD operations using Dapper |
| **DicomStudyModels** | Data transfer object for study metadata |
| **Storage/dicom_files** | Local file system storage for uploaded DICOM files |

---

## 🔄 Architecture & Flow

### High-Level Architecture

```
┌─────────────┐     ┌──────────────┐     ┌────────────────┐     ┌─────────────────┐
│   Client    │────▶│ DicomController│───▶│ DicomHapper    │────▶│ DicomViewerRepo │
│ (Postman/   │     │ (API Layer)   │     │ (DICOM Parser) │     │ (Data Access)   │
│  Swagger)   │     │               │     │                │     │                 │
└─────────────┘     └──────────────┘     └────────────────┘     └────────┬────────┘
                                                                          │
                                                                          ▼
                                                               ┌─────────────────┐
                                                               │  SQL Server     │
                                                               │  (DicomViewerDb)│
                                                               └─────────────────┘
```

### Detailed Flow: **POST /api/Dicom/PostStudy** (Upload DICOM)

```
1. Client sends multipart/form-data with .dcm file
         │
         ▼
2. DicomController.PostStudy(IFormFile file)
   - Validates file exists and has content
   - Creates Storage/dicom_files directory if not exists
   - Generates unique filename: {Guid}_{originalFilename}
   - Saves file to local storage
         │
         ▼
3. DicomHapper.ProcessAndSave(filePath, configuration)
   - Opens DICOM file using fo-dicom: DicomFile.Open(path)
   - Extracts metadata from Dataset:
     • StudyInstanceUID (0020,000D)
     • PatientName (0010,0010)
     • PatientID (0010,0020)
     • Modality (0008,0060)
     • StudyDate (0008,0020)
   - Creates DicomStudyModels entity
         │
         ▼
4. DicomViewerRepo.PostStudy(study)
   - Opens SQL connection (DefaultConnection)
   - Executes INSERT INTO DicomStudies...
   - Returns affected rows count
         │
         ▼
5. Returns 200 OK with success message
```

### Flow: **GET /api/Dicom/GetAllStudies**

```
Client → Controller → Repository → SQL Query (SELECT * FROM DicomStudies)
                    ← Returns List<DicomStudyModels> ←
```

### Flow: **GET /api/Dicom/GetStudyByUID/{studyInstanceUID}**

```
Client → Controller → Repository → SQL Query (WHERE StudyInstanceUID = @uid)
                    ← Returns IEnumerable<DicomStudyModels> ←
```

### Flow: **DELETE /api/Dicom/DeleteStudy/{studyInstanceUID}**

```
Client → Controller → Repository → SQL Query (DELETE WHERE StudyInstanceUID = @uid)
                    ← Returns affected rows (0 = Not Found) ←
```

---

## 🌐 API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/Dicom/GetAllStudies` | Retrieve all DICOM studies |
| `GET` | `/api/Dicom/GetStudyByUID/{studyInstanceUID}` | Get study by StudyInstanceUID |
| `POST` | `/api/Dicom/PostStudy` | Upload and process a DICOM file |
| `DELETE` | `/api/Dicom/DeleteStudy/{studyInstanceUID}` | Delete a study by StudyInstanceUID |

### Request/Response Examples

#### POST /api/Dicom/PostStudy
**Request:** `multipart/form-data`
- `file`: `.dcm` file (required)

**Response (200 OK):**
```json
{
  "message": "Dicom File Save Successfully"
}
```

**Response (400 Bad Request):**
```json
"File Not Provided"
```

#### GET /api/Dicom/GetAllStudies
**Response (200 OK):**
```json
[
  {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "studyInstanceUID": "1.2.840.113619.2.5.1.3289.4567.20210427.142013",
    "patientName": "Doe^John",
    "patientID": "MRN123456",
    "modality": "MR",
    "studyDate": "20210427",
    "filePath": "C:\\Dicom_Viewer\\Storage\\dicom_files\\uuid_filename.dcm"
  }
]
```

#### GET /api/Dicom/GetStudyByUID/{studyInstanceUID}
**Response (200 OK):**
```json
[
  {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "studyInstanceUID": "1.2.840.113619.2.5.1.3289.4567.20210427.142013",
    "patientName": "Doe^John",
    "patientID": "MRN123456",
    "modality": "MR",
    "studyDate": "20210427",
    "filePath": "C:\\Dicom_Viewer\\Storage\\dicom_files\\uuid_filename.dcm"
  }
]
```

#### DELETE /api/Dicom/DeleteStudy/{studyInstanceUID}
**Response (200 OK):**
```json
{
  "message": "Study deleted successfully."
}
```

**Response (404 Not Found):**
```json
"Study not found."
```

---

## 🗄️ Database Schema

### Table: `DicomStudies`

```sql
CREATE TABLE DicomStudies (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    StudyInstanceUID NVARCHAR(256) NOT NULL,
    PatientName NVARCHAR(256),
    PatientID NVARCHAR(128),
    Modality NVARCHAR(64),
    StudyDate NVARCHAR(16),
    FilePath NVARCHAR(512)
);

-- Optional: Index for faster lookups
CREATE INDEX IX_DicomStudies_StudyInstanceUID ON DicomStudies(StudyInstanceUID);
```

### Entity Mapping (DicomStudyModels.cs)

```csharp
public class DicomStudyModels
{
    public Guid Id { get; set; }                    // PK
    public string StudyInstanceUID { get; set; }    // DICOM Tag (0020,000D)
    public string PatientName { get; set; }         // DICOM Tag (0010,0010)
    public string PatientID { get; set; }           // DICOM Tag (0010,0020)
    public string Modality { get; set; }            // DICOM Tag (0008,0060)
    public string StudyDate { get; set; }           // DICOM Tag (0008,0020)
    public string FilePath { get; set; }            // Local file path
}
```

---

## 📄 Example DICOM Files

Sample DICOM files used for testing were sourced from **[Siemens Healthineers Magnetom World - DICOM Images](https://www.magnetomworld.siemens-healthineers.com/clinical-corner/protocols/dicom-images)**.

> **Note:** Sample DICOM files are not stored in the repository due to size. Download them directly from the [Siemens Healthineers DICOM Images page](https://www.magnetomworld.siemens-healthineers.com/clinical-corner/protocols/dicom-images) or use your own `.dcm` files.

### Sample Files Used in Testing

| File | Size | Modality | Description |
|------|------|----------|-------------|
| `Vida_Head.MR.Comp_DR-Gain_DR.1005.1.2021.04.27.14.20.13.818.14380335.dcm` | ~520 KB | MR | MRI Head Study |
| `IMG-0001-00001.dcm` | ~27 MB | CT | Generic CT Image |

### Expected DICOM Tags Extracted

The API extracts the following standard DICOM tags from uploaded files:

| Tag | Keyword | VR | Description |
|-----|---------|----|-------------|
| (0010,0010) | PatientName | PN | Patient's full name |
| (0010,0020) | PatientID | LO | Patient ID / Medical Record Number |
| (0020,000D) | StudyInstanceUID | UI | Unique identifier for the study |
| (0008,0060) | Modality | CS | Imaging modality (MR, CT, XA, etc.) |
| (0008,0020) | StudyDate | DA | Date of the study (YYYYMMDD) |

### Testing with Your Own DICOM Files

Place any `.dcm` file in the storage folder or upload via the API:

```bash
# Create the storage directory (if not exists)
mkdir -p "Dicom Viewer/Storage/dicom_files"

# Copy your DICOM files here
cp /path/to/your/file.dcm "Dicom Viewer/Storage/dicom_files/"
```

Then test via:
- **Swagger UI:** `https://localhost:7xxx/swagger` → POST `/api/Dicom/PostStudy`
- **HTTP file:** Open `Dicom Viewer/Dicom Viewer.http` and send the POST request

---

## ✅ Prerequisites

- **.NET 8.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **SQL Server** (Express, LocalDB, or full) - [Download](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- **Visual Studio 2022** or **VS Code** with C# extension
- **Git** (optional)

---

## ⚙️ Setup & Installation

### 1. Clone the Repository
```bash
git clone <repository-url>
cd Dicom_Viewer
```

### 2. Configure Database Connection

Edit `Dicom Viewer/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=yourserver;Database=DicomViewerDb;Trusted_Connection=true;TrustServerCertificate=true"
  }
}
```

**Connection String Options:**
- **SQL Express (default):** `Server=.\\SQLEXPRESS;Database=DicomViewerDb;Trusted_Connection=true;TrustServerCertificate=true`
- **LocalDB:** `Server=(localdb)\\MSSQLLocalDB;Database=DicomViewerDb;Trusted_Connection=true;`
- **Docker:** `Server=localhost,1433;Database=DicomViewerDb;User=sa;Password=YourPassword123;TrustServerCertificate=true`

### 3. Create Database & Table

Run this SQL script in SSMS or via `sqlcmd`:

```sql
CREATE DATABASE DicomViewerDb;
GO

USE DicomViewerDb;
GO

CREATE TABLE DicomStudies (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    StudyInstanceUID NVARCHAR(256) NOT NULL,
    PatientName NVARCHAR(256),
    PatientID NVARCHAR(128),
    Modality NVARCHAR(64),
    StudyDate NVARCHAR(16),
    FilePath NVARCHAR(512)
);

CREATE INDEX IX_DicomStudies_StudyInstanceUID ON DicomStudies(StudyInstanceUID);
```

### 4. Restore Dependencies
```bash
cd "Dicom Viewer"
dotnet restore
```

---

## 🚀 Running the Application

### Option 1: Visual Studio
1. Open `Dicom Viewer.slnx`
2. Set `Dicom Viewer` as startup project
3. Press `F5` or `Ctrl+F5`

### Option 2: Command Line
```bash
cd "Dicom Viewer"
dotnet run
```

### Option 3: Docker (if Dockerfile added)
```bash
docker build -t dicom-viewer .
docker run -p 8080:8080 dicom-viewer
```

**Default URLs:**
- HTTP: `http://localhost:5xxx`
- HTTPS: `https://localhost:7xxx`
- Swagger UI: `https://localhost:7xxx/swagger`

---

## 🧪 Testing with Swagger

1. Navigate to `https://localhost:7xxx/swagger`
2. Explore available endpoints:
   - **GET /api/Dicom/GetAllStudies** - Click "Try it out" → "Execute"
   - **POST /api/Dicom/PostStudy** - Click "Try it out" → Choose `.dcm` file → "Execute"
   - **GET /api/Dicom/GetStudyByUID/{uid}** - Enter StudyInstanceUID → "Execute"
   - **DELETE /api/Dicom/DeleteStudy/{uid}** - Enter StudyInstanceUID → "Execute"

### Using the `.http` File (VS Code REST Client / Rider / VS 2022)

Open `Dicom Viewer/Dicom Viewer.http` and click "Send Request" above each endpoint.

---

## 📦 Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| **fo-dicom** | 5.2.6 | DICOM file parsing & metadata extraction |
| **Microsoft.EntityFrameworkCore.SqlServer** | 8.0.31 | EF Core SQL Server provider (for future migrations) |
| **System.Data.SqlClient** | 4.9.1 | ADO.NET SQL Server driver |
| **Dapper** (via ExpressionExtensionSQL.Dapper) | 1.2.3 | Micro-ORM for fast data access |
| **Swashbuckle.AspNetCore** | 6.6.2 | Swagger/OpenAPI documentation |
| **ExpressionExtensionSQL.Dapper** | 1.2.3 | Extended Dapper functionality |

### Installing/Updating Packages
```bash
cd "Dicom Viewer"
dotnet add package fo-dicom --version 5.2.6
dotnet add package Swashbuckle.AspNetCore --version 6.6.2
dotnet add package Dapper --version 2.1.35
```

---

## 🔧 Configuration Details

### appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=DicomViewerDb;Trusted_Connection=true;TrustServerCertificate=true"
  }
}
```

### CORS Policy (Program.cs)
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});
```
> **Note:** For production, restrict origins to specific domains.

---
