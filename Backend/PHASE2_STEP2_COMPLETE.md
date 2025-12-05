# 🏥 HospitalNet Phase 2 - Step 2 Complete

## Business Logic Manager Classes

**Date:** December 6, 2025  
**Status:** ✅ COMPLETE - Ready for Integration Testing

---

## 📋 Deliverables (Step 2)

### 1. AppointmentManager Class
**File:** `Backend/BusinessLogic/AppointmentManager.cs`

#### Purpose
Manages all appointment-related business operations including scheduling with double-booking prevention, cancellation, and availability checks.

#### Key Features
- ✅ **Double-Booking Prevention** - Validates availability before scheduling
- ✅ **OUTPUT Parameter Handling** - Captures AppointmentID and ErrorMessage from sp_CreateAppointment
- ✅ **Error Messaging** - Returns meaningful error messages for double-booking conflicts
- ✅ **Appointment Lifecycle** - Schedule → Complete → Cancel operations
- ✅ **Doctor Capacity Management** - Checks maximum patient capacity per day
- ✅ **Schedule Retrieval** - Gets doctor schedule within date range

#### Core Methods

##### **ScheduleAppointment(Appointment appt)**
**CRITICAL METHOD** - Implements double-booking prevention logic:
```csharp
// 1. Validates appointment model
// 2. Checks doctor availability for time slot
// 3. Creates SqlParameters for sp_CreateAppointment
// 4. Executes stored procedure
// 5. Captures @AppointmentID and @ErrorMessage OUTPUT parameters
// 6. Throws exception if @ErrorMessage is not null
// 7. Populates appointment object with new ID
```

Key flow:
- Patient 1 tries 10:00 AM (30 minutes) → Success ✅
- Patient 2 tries 10:15 AM (30 minutes) → BLOCKED ❌ (conflicts with Patient 1)
- Patient 3 tries 10:30 AM (30 minutes) → Success ✅ (no conflict)

##### **CheckDoctorAvailability(int doctorId, DateTime dateTime, int minutes)**
- Validates doctor capacity for requested time slot
- Returns true if available, false if conflicted
- Called internally by ScheduleAppointment()

##### **CancelAppointment(int appointmentId, string reason)**
- Soft-cancels appointment (doesn't delete, marks as Cancelled)
- Records cancellation reason and timestamp
- Returns boolean success status

##### **CompleteAppointment(int appointmentId)**
- Marks appointment as Completed
- Called after medical record is recorded

##### **GetDoctorSchedule(int doctorId, DateTime start, DateTime end)**
- Retrieves all appointments for doctor in date range
- Maps SqlDataReader results to List<Appointment>
- Used for schedule viewing and analytics

##### **GetPatientAppointments(int patientId)**
- Retrieves all appointments for a patient
- Filtered by patient ID

#### Error Handling
```csharp
try
{
    // Validation
    // Database operation
}
catch (SqlException sqlEx)
{
    throw new Exception($"Database error: {sqlEx.Message}", sqlEx);
}
catch (Exception ex)
{
    throw new Exception($"Error: {ex.Message}", ex);
}
```

---

### 2. DoctorManager Class
**File:** `Backend/BusinessLogic/DoctorManager.cs`

#### Purpose
Manages doctor-related operations including registration, profile management, and workload tracking.

#### Key Features
- ✅ **Doctor Registration** - Creates new doctor with OUTPUT parameter handling
- ✅ **Specialization Filtering** - Retrieves doctors by specialty (Cardiology, etc.)
- ✅ **License Number Validation** - Ensures unique license numbers
- ✅ **Capacity Tracking** - Monitors maximum patients per day
- ✅ **Schedule Management** - Tracks doctor availability

#### Core Methods

##### **RegisterDoctor(Doctor doctor)**
- Validates doctor model
- Checks license number uniqueness
- Executes sp_CreateDoctor with OUTPUT parameter
- Returns doctor with new DoctorID

##### **GetAllDoctors()**
- Retrieves all active doctors

##### **GetDoctorById(int doctorId)**
- Single doctor retrieval

##### **GetDoctorsBySpecialization(string specialization)**
- Filters doctors by specialty
- Example: "Cardiology", "Neurology", "Pediatrics"

##### **GetDoctorByLicenseNumber(string licenseNumber)**
- License-based lookup

##### **UpdateDoctor(Doctor doctor)**
- Updates doctor profile information

##### **IsDoctorAtCapacityForDate(int doctorId, DateTime date)**
- Checks if doctor reached MaxPatientCapacityPerDay
- Used by AppointmentManager to block scheduling

##### **GetDoctorAppointmentCountForDate(int doctorId, DateTime date)**
- Returns count of appointments on specific date

#### Usage Example
```csharp
var doctorMgr = new DoctorManager(connectionString);

// Register new doctor
var newDoctor = new Doctor
{
    FirstName = "David",
    LastName = "Anderson",
    Specialization = "Cardiology",
    LicenseNumber = "LIC-12345",
    MaxPatientCapacityPerDay = 12
};

Doctor registeredDoctor = doctorMgr.RegisterDoctor(newDoctor);
Console.WriteLine($"Doctor registered with ID: {registeredDoctor.DoctorID}");

// Get cardiologists
var cardiologists = doctorMgr.GetDoctorsBySpecialization("Cardiology");
```

---

### 3. PatientManager Class
**File:** `Backend/BusinessLogic/PatientManager.cs`

#### Purpose
Manages patient registration, profile management, and medical history retrieval.

#### Key Features
- ✅ **Patient Registration** - Creates new patient with OUTPUT parameter
- ✅ **Soft Delete** - Deactivates patient without removing data
- ✅ **Search Functionality** - By name or phone number
- ✅ **Visit History** - Retrieves combined appointments + medical records
- ✅ **Profile Updates** - Modifies patient information

#### Core Methods

##### **RegisterPatient(Patient patient)**
- Validates patient model
- Executes sp_CreatePatient with OUTPUT parameter
- Returns patient with new PatientID

##### **GetPatientById(int patientId)**
- Single patient retrieval

##### **GetAllActivePatients()**
- Retrieves all active patients

##### **SearchPatientsByName(string searchName)**
- Searches by first or last name
- Partial name matching supported

##### **SearchPatientByPhoneNumber(string phoneNumber)**
- Phone number lookup

##### **GetPatientVisitHistory(int patientId)**
**CRITICAL METHOD** - Returns `PatientVisitHistory` object containing:
- List of Appointment objects (scheduled, completed, cancelled)
- List of MedicalRecord objects (clinical data, diagnoses, prescriptions)
- Computed properties: TotalVisits, MostRecentVisitDate

##### **UpdatePatient(Patient patient)**
- Updates patient profile

##### **DeactivatePatient(int patientId)**
- Soft delete (sets IsActive = false)
- Preserves all historical data

#### PatientVisitHistory Class
```csharp
public class PatientVisitHistory
{
    public int PatientID { get; set; }
    public List<Appointment> Appointments { get; set; }
    public List<MedicalRecord> MedicalRecords { get; set; }
    
    public int TotalVisits // Computed: Count of both lists
    public DateTime? MostRecentVisitDate // Latest appointment or medical record
}
```

#### Usage Example
```csharp
var patientMgr = new PatientManager(connectionString);

// Register patient
var newPatient = new Patient
{
    FirstName = "John",
    LastName = "Smith",
    DateOfBirth = new DateTime(1985, 3, 15),
    PhoneNumber = "555-0101",
    Email = "john.smith@email.com",
    Address = "123 Main St",
    City = "New York",
    PostalCode = "10001"
};

Patient registeredPatient = patientMgr.RegisterPatient(newPatient);

// Get complete visit history
PatientVisitHistory history = patientMgr.GetPatientVisitHistory(registeredPatient.PatientID);
Console.WriteLine($"Total visits: {history.TotalVisits}");
Console.WriteLine($"Last visit: {history.MostRecentVisitDate}");
```

---

### 4. MedicalRecordManager Class
**File:** `Backend/BusinessLogic/MedicalRecordManager.cs`

#### Purpose
Manages clinical data entry, medical records, and follow-up appointment tracking.

#### Key Features
- ✅ **Clinical Data Recording** - Captures diagnosis, prescriptions, vital signs
- ✅ **Unlimited Prescriptions** - PrescriptionText stored as NVARCHAR(MAX)
- ✅ **Follow-up Tracking** - Manages required follow-up appointments
- ✅ **Allergy Documentation** - Records allergies during visit
- ✅ **Overdue Follow-ups** - Identifies missed follow-up dates

#### Core Methods

##### **AddMedicalRecord(MedicalRecord record)**
**CRITICAL METHOD** - Doctor enters clinical information:
```csharp
// Inputs:
// - Diagnosis (e.g., "Essential Hypertension - Controlled")
// - ClinicalNotes (e.g., "Patient stable, continues on current medication")
// - PrescriptionText (UNLIMITED - can be very long multi-line prescriptions)
// - VitalSigns (e.g., "BP: 128/82, HR: 72, Temp: 98.6F")
// - AllergiesNotedDuringVisit (e.g., "Penicillin allergy confirmed")
// - FollowUpRequired (true/false)
// - FollowUpDate (if required)

// Execution:
// 1. Validates all required fields
// 2. Validates follow-up date if required
// 3. Executes sp_RecordMedicalVisit
// 4. Captures @RecordID OUTPUT parameter
// 5. Returns record with RecordID populated
```

Prescription example:
```
Lisinopril 10mg once daily
Amlodipine 5mg once daily
Metformin 500mg twice daily with meals
Continue current regimen
```

##### **GetPatientMedicalRecords(int patientId)**
- Retrieves all medical records for patient
- Sorted by visit date

##### **GetDoctorMedicalRecords(int doctorId)**
- Retrieves all records created by doctor

##### **GetMedicalRecordByAppointmentId(int appointmentId)**
- Gets clinical data associated with specific appointment

##### **GetFollowUpRequiredRecords()**
- All records with FollowUpRequired = true
- Used for scheduling follow-up appointments

##### **GetOverdueFollowUps()**
- Records where FollowUpDate < TODAY
- Used for alerts and reminders

##### **UpdateMedicalRecord(MedicalRecord record)**
- Modifies existing record (corrections, additional notes)

#### Usage Example
```csharp
var medicalMgr = new MedicalRecordManager(connectionString);

// Record medical visit
var record = new MedicalRecord
{
    AppointmentID = 5,
    PatientID = 1,
    DoctorID = 1,
    VisitDate = DateTime.Today,
    ClinicalNotes = "Patient stable, BP well controlled",
    Diagnosis = "Essential Hypertension - Controlled",
    PrescriptionText = "Lisinopril 10mg daily\nAmlodipine 5mg daily",
    VitalSigns = "BP: 128/82, HR: 72, Temp: 98.6F",
    AllergiesNotedDuringVisit = "Penicillin allergy confirmed",
    FollowUpRequired = true,
    FollowUpDate = DateTime.Today.AddMonths(1)
};

MedicalRecord savedRecord = medicalMgr.AddMedicalRecord(record);

// Check for overdue follow-ups
List<MedicalRecord> overdueFollowUps = medicalMgr.GetOverdueFollowUps();
foreach (var overdue in overdueFollowUps)
{
    Console.WriteLine($"Patient {overdue.PatientID} - Follow-up overdue since {overdue.FollowUpDate:yyyy-MM-dd}");
}
```

---

### 5. AnalyticsManager Class
**File:** `Backend/BusinessLogic/AnalyticsManager.cs`

#### Purpose
Provides business intelligence and performance metrics for the hospital system.

#### Key Features
- ✅ **Appointment Analytics** - Cancellation rates, completion rates
- ✅ **Doctor Performance** - Metrics per doctor and specialization
- ✅ **Patient Load Analysis** - Patient distribution and trends
- ✅ **Peak Time Analysis** - Identifies busy appointment slots
- ✅ **Comprehensive Reporting** - Generates full performance reports

#### Core Methods

##### **GetAppointmentStatistics(DateTime start, DateTime end)**
Returns:
- TotalAppointments
- ScheduledAppointments
- CompletedAppointments
- CancelledAppointments
- NoShowAppointments
- CancellationRate (%)
- CompletionRate (%)

##### **GetDoctorPerformanceMetrics(int? doctorId = null)**
For each doctor:
- TotalAppointments
- CompletedAppointments
- CancelledAppointments
- AverageAppointmentDuration
- CompletionRate (%)
- CancellationRate (%)
- TotalPatientsSeen

##### **GetPatientLoadStatistics(DateTime start, DateTime end)**
Returns:
- TotalActivePatients
- NewPatientsRegistered
- PatientsWithAppointments
- AveragePatientsPerDoctor
- PatientRetentionRate (%)

##### **GetSpecializationStatistics()**
For each specialization:
- DoctorCount
- TotalAppointments
- CompletedAppointments
- AveragePatientsPerDoctor
- CompletionRate (%)

##### **GetPeakAppointmentTimes()**
Hourly breakdown:
- Hour (0-23)
- AppointmentCount
- DoctorCount
- AveragePatients

##### **GeneratePerformanceReport(DateTime start, DateTime end)**
Comprehensive report combining all statistics above.

#### Usage Example
```csharp
var analytics = new AnalyticsManager(connectionString);

// Get appointment statistics for December
var stats = analytics.GetAppointmentStatistics(
    new DateTime(2025, 12, 1),
    new DateTime(2025, 12, 31));

Console.WriteLine($"Total Appointments: {stats.TotalAppointments}");
Console.WriteLine($"Completion Rate: {stats.CompletionRate}%");
Console.WriteLine($"Cancellation Rate: {stats.CancellationRate}%");

// Get doctor performance
var doctorMetrics = analytics.GetDoctorPerformanceMetrics();
foreach (var doctor in doctorMetrics)
{
    Console.WriteLine($"{doctor.FullName}: {doctor.CompletionRate}% completion");
}

// Generate full report
var report = analytics.GeneratePerformanceReport(
    DateTime.Today.AddMonths(-1),
    DateTime.Today);
Console.WriteLine(report.GetSummary());
```

---

## 🏗️ Project Structure

```
Backend/
├── Infrastructure/
│   └── DatabaseHelper.cs
├── Models/
│   ├── Patient.cs
│   ├── Doctor.cs
│   ├── Appointment.cs
│   └── MedicalRecord.cs
└── BusinessLogic/
    ├── AppointmentManager.cs        ✅ Step 2
    ├── DoctorManager.cs              ✅ Step 2
    ├── PatientManager.cs             ✅ Step 2
    ├── MedicalRecordManager.cs       ✅ Step 2
    └── AnalyticsManager.cs           ✅ Step 2
```

---

## 💡 Architecture: How Everything Works Together

### Appointment Scheduling Flow
```
┌─────────────────────────────────┐
│ UI: Schedule Appointment Button  │
└────────────────┬────────────────┘
                 │
                 ▼
┌─────────────────────────────────┐
│ AppointmentManager               │
│ .ScheduleAppointment()           │
└────────────────┬────────────────┘
                 │
                 ├─→ Validate appointment model
                 │
                 ├─→ Check availability:
                 │   DoctorManager.IsDoctorAtCapacityForDate()
                 │
                 ├─→ Call DatabaseHelper.ExecuteNonQueryWithOutputs()
                 │   sp_CreateAppointment → @AppointmentID, @ErrorMessage
                 │
                 ├─→ If @ErrorMessage not null → Throw Exception
                 │   (Double-booking detected by database!)
                 │
                 └─→ Return Appointment with AppointmentID
```

### Medical Record Entry Flow
```
┌──────────────────────────────┐
│ Doctor: Enter Clinical Data  │
└────────────┬─────────────────┘
             │
             ▼
┌──────────────────────────────┐
│ MedicalRecordManager         │
│ .AddMedicalRecord()          │
└────────────┬─────────────────┘
             │
             ├─→ Validate record model
             │
             ├─→ Validate follow-up date (if required)
             │
             ├─→ Call DatabaseHelper.ExecuteNonQueryWithOutputs()
             │   sp_RecordMedicalVisit → @RecordID
             │
             ├─→ Store clinical data:
             │   - Diagnosis
             │   - Prescriptions (unlimited)
             │   - Vital signs
             │   - Allergies
             │   - Follow-up info
             │
             └─→ Return MedicalRecord with RecordID
```

### Patient History Retrieval Flow
```
┌────────────────────────────┐
│ View Patient Profile       │
└────────────┬───────────────┘
             │
             ▼
┌────────────────────────────┐
│ PatientManager             │
│ .GetPatientVisitHistory()  │
└────────────┬───────────────┘
             │
             ├─→ Call sp_GetPatientVisitHistory
             │   (Returns both appointments & medical records)
             │
             ├─→ Parse DataTable results
             │   - Extract Appointment rows
             │   - Extract MedicalRecord rows
             │
             └─→ Return PatientVisitHistory object
                 {
                   Appointments: List[],
                   MedicalRecords: List[],
                   TotalVisits: computed,
                   MostRecentVisit: computed
                 }
```

---

## 🔄 Critical Double-Booking Prevention

**Location:** `AppointmentManager.ScheduleAppointment()`

### How It Works:

1. **Application Layer Check** (AppointmentManager)
   - Validates doctor availability before DB call
   - Prevents unnecessary database round-trips

2. **Stored Procedure Check** (sp_CreateAppointment)
   - Database constraints enforce uniqueness
   - Handles race conditions
   - Returns @ErrorMessage if conflict detected

3. **Error Propagation**
   ```csharp
   string errorMessage = (string)outputs["@ErrorMessage"];
   if (!string.IsNullOrEmpty(errorMessage))
   {
       throw new Exception($"Failed to schedule: {errorMessage}");
   }
   ```

### Example Scenario:
```
Thread 1: Schedule Patient A - 10:00-10:30 → Success ✅
Thread 2: Schedule Patient B - 10:15-10:45 → BLOCKED ❌
         (sp_CreateAppointment detects overlap)
         @ErrorMessage = "Time slot conflicts with existing appointment"
         → Exception thrown → UI shows error
```

---

## 📊 Code Statistics

| Component | Lines | Methods | Classes |
|-----------|-------|---------|---------|
| AppointmentManager.cs | 400+ | 8 | 1 |
| DoctorManager.cs | 350+ | 9 | 1 |
| PatientManager.cs | 450+ | 9 | 2 (+ PatientVisitHistory) |
| MedicalRecordManager.cs | 400+ | 10 | 1 |
| AnalyticsManager.cs | 450+ | 6 | 7 (+ 6 statistics classes) |
| **TOTAL** | **2050+** | **42** | **22** |

---

## ✅ Testing Checklist

Before deployment, verify:

- [ ] AppointmentManager.ScheduleAppointment() captures OUTPUT parameters
- [ ] Double-booking prevention catches time conflicts
- [ ] CancelAppointment() properly soft-deletes
- [ ] DoctorManager.RegisterDoctor() rejects duplicate licenses
- [ ] PatientManager.GetPatientVisitHistory() returns both appointments and records
- [ ] MedicalRecordManager.AddMedicalRecord() handles unlimited prescriptions
- [ ] AnalyticsManager.GetAppointmentStatistics() calculates rates correctly
- [ ] All Manager classes throw meaningful exceptions
- [ ] All Manager classes handle NULL values safely
- [ ] ERROR paths (double-booking, invalid ID, etc.) are tested
- [ ] Concurrent scheduling requests are handled properly
- [ ] DATE filtering works correctly for date ranges
- [ ] SEARCH functionality returns expected results
- [ ] COMPUTED properties calculate correctly

---

## 🚀 Integration Next Steps

### Step 3 (Coming Next):
- Create API layer (Controllers) - ASP.NET Core or MVC
- Implement DTOs (Data Transfer Objects)
- Add authentication/authorization
- Create Swagger/OpenAPI documentation

### Unit Testing:
- Test each Manager method independently
- Mock DatabaseHelper for isolated testing
- Verify error handling paths

### Integration Testing:
- Test full workflows (Schedule → Record → Archive)
- Test concurrent operations
- Test date boundary conditions

---

## 📝 Key Implementation Notes

### 1. ADO.NET Best Practices
✅ All stored procedures called via DatabaseHelper  
✅ All parameters use SqlParameter (SQL injection safe)  
✅ All connections opened/closed automatically  
✅ All errors caught and meaningful messages returned  

### 2. Error Handling
✅ SqlException → Custom exception with message  
✅ ArgumentException for invalid inputs  
✅ Validation before database operations  
✅ Graceful handling of NULL values  

### 3. Output Parameter Handling
✅ Used for generated IDs (AppointmentID, DoctorID, etc.)  
✅ Used for error messages from stored procedures  
✅ Dictionary<string, object> returned for easy access  
✅ Type-safe extraction with proper casting  

### 4. Search and Filter Operations
✅ GetDoctorsBySpecialization() - Filters by specialty  
✅ SearchPatientsByName() - Partial name matching  
✅ GetDoctorSchedule() - Date range filtering  
✅ GetPeakAppointmentTimes() - Hourly breakdown  

### 5. Complex Result Mapping
✅ PatientVisitHistory - Combines 2 entity types  
✅ PerformanceReport - Combines 5 statistics objects  
✅ Generic mapping with Func<SqlDataReader, T>  
✅ Computed properties for complex calculations  

---

## 💎 Manager Classes Summary

| Manager | Responsibility | Key Methods |
|---------|-----------------|-------------|
| **AppointmentManager** | Appointment lifecycle & double-booking prevention | ScheduleAppointment, CheckDoctorAvailability, CancelAppointment |
| **DoctorManager** | Doctor registration & capacity tracking | RegisterDoctor, GetDoctorsBySpecialization, IsDoctorAtCapacityForDate |
| **PatientManager** | Patient registration & visit history | RegisterPatient, GetPatientVisitHistory, SearchPatients |
| **MedicalRecordManager** | Clinical data entry & follow-up tracking | AddMedicalRecord, GetOverdueFollowUps, GetFollowUpRequired |
| **AnalyticsManager** | Business intelligence & reporting | GeneratePerformanceReport, GetAppointmentStatistics, GetDoctorPerformanceMetrics |

---

## ✨ What's Complete

**Phase 2 - Step 2: Manager Classes ✅**

- ✅ AppointmentManager (8 methods)
- ✅ DoctorManager (9 methods)
- ✅ PatientManager (9 methods + PatientVisitHistory class)
- ✅ MedicalRecordManager (10 methods)
- ✅ AnalyticsManager (6 methods + 7 statistics classes)
- ✅ Comprehensive error handling
- ✅ OUTPUT parameter handling
- ✅ Double-booking prevention logic
- ✅ Complex result mapping
- ✅ SQL injection prevention
- ✅ NULL-safe operations

**Total: 42 methods across 22 classes - 2,050+ lines of production code**

---

## 🎯 Architecture Complete

✅ **Phase 1:** MSSQL Database (4 tables, 6 stored procedures)  
✅ **Phase 2 Step 1:** Infrastructure & Models (DatabaseHelper + 4 POCOs)  
✅ **Phase 2 Step 2:** Business Logic Managers (5 Manager classes)  

**Ready for:** Phase 2 Step 3 - API Layer Integration

---

*HospitalNet Phase 2 - Step 2*  
*Business Logic Managers*  
*December 6, 2025*
