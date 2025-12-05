# HospitalNet - Phase 1 Database Design Documentation

## Overview
This document provides a detailed explanation of the HospitalNet database schema designed for Phase 1. The system uses MSSQL Server with manually written SQL queries (no ORM) to ensure strong data integrity and prevent critical issues like double-booking.

---

## Database Structure

### 1. **Patients Table**
Stores comprehensive patient information for the hospital.

**Columns:**
- `PatientID` (INT, PK) - Unique identifier, auto-increment
- `FirstName` (NVARCHAR(100)) - Patient's first name
- `LastName` (NVARCHAR(100)) - Patient's last name
- `DateOfBirth` (DATE) - Patient's date of birth
- `Gender` (NVARCHAR(10)) - Gender with constraint: 'Male', 'Female', 'Other'
- `PhoneNumber` (NVARCHAR(15)) - Contact phone number
- `Email` (NVARCHAR(100)) - Contact email address
- `Address` (NVARCHAR(500)) - Street address
- `City` (NVARCHAR(100)) - City of residence
- `PostalCode` (NVARCHAR(20)) - Postal code
- `InsuranceProviderID` (NVARCHAR(50)) - Insurance provider identifier
- `MedicalHistorySummary` (NVARCHAR(MAX)) - Summary of medical history
- `IsActive` (BIT) - Soft delete flag (1 = active, 0 = inactive)
- `CreatedDate` (DATETIME) - Record creation timestamp
- `UpdatedDate` (DATETIME) - Last update timestamp
- `LastVisitDate` (DATE) - Date of last appointment

**Indexes:**
- Non-clustered index on `(PhoneNumber, Email)` for quick lookups
- Non-clustered index on `(IsActive)` for filtering active patients

---

### 2. **Doctors Table**
Stores doctor information and credentials.

**Columns:**
- `DoctorID` (INT, PK) - Unique identifier, auto-increment
- `FirstName` (NVARCHAR(100)) - Doctor's first name
- `LastName` (NVARCHAR(100)) - Doctor's last name
- `Specialization` (NVARCHAR(100)) - Medical specialization (e.g., Cardiology, Pediatrics)
- `LicenseNumber` (NVARCHAR(50)) - Medical license number (UNIQUE)
- `PhoneNumber` (NVARCHAR(15)) - Contact phone number
- `Email` (NVARCHAR(100)) - Contact email (UNIQUE)
- `OfficeLocation` (NVARCHAR(200)) - Office location or room number
- `YearsOfExperience` (INT) - Years of medical practice
- `MaxPatientCapacityPerDay` (INT) - Maximum appointments per day
- `IsActive` (BIT) - Active status (1 = active, 0 = inactive)
- `CreatedDate` (DATETIME) - Record creation timestamp
- `UpdatedDate` (DATETIME) - Last update timestamp

**Indexes:**
- Non-clustered index on `(Specialization)` for filtering by specialty
- Non-clustered index on `(IsActive)` for active doctors

**Constraints:**
- Unique on `LicenseNumber` - Ensures medical licenses are not duplicated
- Unique on `Email` - Ensures unique email addresses

---

### 3. **Appointments Table** ⭐ CRITICAL
Manages appointment scheduling with built-in double-booking prevention.

**Columns:**
- `AppointmentID` (INT, PK) - Unique identifier, auto-increment
- `PatientID` (INT, FK) - References Patients table
- `DoctorID` (INT, FK) - References Doctors table
- `AppointmentDateTime` (DATETIME) - Date and time of appointment
- `DurationMinutes` (INT) - Appointment duration in minutes (default 30)
- `ReasonForVisit` (NVARCHAR(500)) - Chief complaint or reason
- `Status` (NVARCHAR(50)) - Appointment status: 'Scheduled', 'Completed', 'Cancelled', 'No-Show'
- `Notes` (NVARCHAR(MAX)) - Additional notes
- `CancellationReason` (NVARCHAR(500)) - Reason if cancelled
- `CancellationDateTime` (DATETIME) - When the appointment was cancelled
- `CreatedDate` (DATETIME) - Record creation timestamp
- `UpdatedDate` (DATETIME) - Last update timestamp

**Foreign Keys:**
- `FK_Appointments_Patient` → Patients(PatientID) with CASCADE delete
- `FK_Appointments_Doctor` → Doctors(DoctorID) with RESTRICT delete

**Critical Constraint - Double-Booking Prevention:**
```
CONSTRAINT UQ_DoctorTimeSlot UNIQUE (DoctorID, AppointmentDateTime)
```
This UNIQUE constraint ensures a doctor cannot have two appointments scheduled at the same exact time. This is the first line of defense against double-booking.

**Indexes:**
- Non-clustered index on `(PatientID)` - Fast lookup of patient's appointments
- Non-clustered index on `(DoctorID, AppointmentDateTime)` - Critical for schedule queries
- Non-clustered index on `(Status)` - Filter by appointment status
- Non-clustered index on `(AppointmentDateTime)` - Range queries on dates

---

### 4. **MedicalRecords Table**
Stores clinical information, diagnoses, and prescriptions for completed visits.

**Columns:**
- `RecordID` (INT, PK) - Unique identifier, auto-increment
- `AppointmentID` (INT, FK) - References Appointments table
- `PatientID` (INT, FK) - References Patients table (denormalized for query performance)
- `DoctorID` (INT, FK) - References Doctors table (denormalized for query performance)
- `VisitDate` (DATE) - Date of the visit
- `ClinicalNotes` (NVARCHAR(MAX)) - Detailed clinical observations
- `Diagnosis` (NVARCHAR(500)) - Medical diagnosis
- **`PrescriptionText` (NVARCHAR(MAX))** - Complete prescription details
- `AllergiesNotedDuringVisit` (NVARCHAR(MAX)) - Any allergies identified
- `VitalSigns` (NVARCHAR(500)) - Blood pressure, heart rate, temperature, etc.
- `FollowUpRequired` (BIT) - Whether follow-up appointment is needed
- `FollowUpDate` (DATE) - Scheduled follow-up date if required
- `CreatedDate` (DATETIME) - Record creation timestamp
- `UpdatedDate` (DATETIME) - Last update timestamp

**Foreign Keys:**
- `FK_MedicalRecords_Appointment` → Appointments(AppointmentID) with CASCADE delete
- `FK_MedicalRecords_Patient` → Patients(PatientID) with CASCADE delete
- `FK_MedicalRecords_Doctor` → Doctors(DoctorID) with RESTRICT delete

**Indexes:**
- Non-clustered index on `(PatientID)` - Medical history lookups
- Non-clustered index on `(DoctorID)` - Doctor's records
- Non-clustered index on `(VisitDate)` - Date-based queries
- Non-clustered index on `(FollowUpRequired, FollowUpDate)` - Filtered for follow-up tracking

---

## Double-Booking Prevention Strategy

### Problem:
Preventing two patients from booking the same doctor at the same time is a critical requirement.

### Solution Stack (Multi-Layer Defense):

#### Layer 1: Database Constraint (UNIQUE)
```sql
CONSTRAINT UQ_DoctorTimeSlot UNIQUE (DoctorID, AppointmentDateTime)
```
- Prevents exactly identical timestamps
- Enforced at the database level
- Immediate UNIQUE CONSTRAINT violation if violated

#### Layer 2: Stored Procedure Validation
`sp_CheckDoctorAvailability` detects **overlapping** time slots (not just identical times):
- Appointment A: 10:00 - 10:30
- Appointment B: 10:15 - 10:45 (Should be rejected)

This procedure checks:
```sql
-- Overlap condition:
-- Existing Start < Requested End AND Existing End > Requested Start
DATEADD(MINUTE, DurationMinutes, AppointmentDateTime) > @AppointmentDateTime
AND AppointmentDateTime < @AppointmentEndTime
```

#### Layer 3: Application Logic (C# Layer - Phase 2)
- Check availability before submitting to database
- Retry logic if race conditions occur
- User-friendly error messages

#### Layer 4: Status Filtering
Cancelled and No-Show appointments are excluded from availability checks, so free slots remain available.

---

## Stored Procedures Overview

### 1. `sp_CheckDoctorAvailability`
**Purpose:** Validates if a doctor is available for an appointment time slot
**Parameters:** DoctorID, AppointmentDateTime, DurationMinutes
**Returns:** IsAvailable (bit), ConflictingAppointmentID (if any)
**Usage:** Called before creating new appointments

### 2. `sp_GetDoctorSchedule`
**Purpose:** Retrieves all non-cancelled appointments for a doctor within a date range
**Parameters:** DoctorID, StartDate, EndDate
**Usage:** Display doctor's calendar/schedule view

### 3. `sp_GetPatientVisitHistory`
**Purpose:** Gets patient's complete appointment and medical record history
**Parameters:** PatientID
**Usage:** Patient records dashboard

### 4. `sp_CreateAppointment`
**Purpose:** Creates appointment with full validation (including availability check)
**Parameters:** PatientID, DoctorID, AppointmentDateTime, DurationMinutes, ReasonForVisit, Notes
**Validation Steps:**
1. Check patient exists
2. Check doctor exists and is active
3. Verify appointment is in the future
4. Call sp_CheckDoctorAvailability
5. Insert into database if all validations pass
**Returns:** AppointmentID or error message

### 5. `sp_RecordMedicalVisit`
**Purpose:** Records clinical information after appointment completion
**Parameters:** AppointmentID, ClinicalNotes, Diagnosis, PrescriptionText, AllergiesNotedDuringVisit, VitalSigns, FollowUpRequired, FollowUpDate
**Side Effects:**
- Creates MedicalRecord
- Updates Appointment status to 'Completed'
- Updates Patient's LastVisitDate

### 6. `sp_CancelAppointment`
**Purpose:** Cancels a scheduled appointment
**Parameters:** AppointmentID, CancellationReason
**Validation:** Only allows cancellation of 'Scheduled' status appointments

---

## Critical SQL Queries

### Query 1: Detect Double-Booking
```sql
SELECT TOP 1
    a.AppointmentID
FROM Appointments a
WHERE a.DoctorID = @DoctorID
  AND a.Status NOT IN ('Cancelled', 'No-Show')
  AND DATEADD(MINUTE, a.DurationMinutes, a.AppointmentDateTime) > @AppointmentDateTime
  AND a.AppointmentDateTime < @AppointmentEndTime;
```

**Explanation:**
- Checks if any non-cancelled appointment overlaps with the requested time
- Overlap logic: Existing_End > Requested_Start AND Existing_Start < Requested_End
- Returns the conflicting AppointmentID if found, NULL if available

---

## Analytics Queries Included

The database supports complex queries for:

1. **Cancellation Rate by Doctor** - Percentage of appointments cancelled per doctor
2. **Patient Load per Doctor** - How many patients and appointment hours per day
3. **Completed Appointments Rate** - Percentage of scheduled appointments actually completed
4. **Medical Records Summary** - Diagnoses and prescriptions by date/doctor
5. **Doctor Workload Analysis** - Hours worked and appointment density
6. **Patient Visit Frequency** - Repeat patient analysis
7. **Double-Booking Conflict Detection** - Data integrity validation query

---

## Data Integrity Measures

1. **Referential Integrity:** Foreign key constraints ensure no orphaned records
2. **Unique Constraints:** License numbers and emails are globally unique
3. **Status Validation:** CHECK constraints on status enums
4. **Cascade/Restrict Deletes:** Prevent accidental data loss
5. **Double-Booking Prevention:** UNIQUE constraint + stored procedure logic
6. **Soft Deletes:** IsActive flags for logical deletion without losing history
7. **Audit Trail:** CreatedDate and UpdatedDate on all tables

---

## Indexes Strategy

**Why These Indexes:**
- Appointment lookups by doctor and date are frequent (frequent with time range queries)
- Patient history queries group by patient
- Status filtering is common
- DateTime range queries require proper indexing for performance

**Index Types:**
- Clustered: Primary key (identity)
- Non-clustered: High-cardinality, frequently searched columns
- Included columns: Reduce key lookups

---

## Scalability Considerations

1. **Partitioning Ready:** AppointmentDateTime is ideal for range-based partitioning
2. **Archive Strategy:** Old MedicalRecords can be archived to separate tables
3. **Denormalization:** PatientID and DoctorID in MedicalRecords speeds queries
4. **Index Coverage:** Covering indexes minimize disk I/O

---

## Implementation Notes for Phase 2 (Backend)

When implementing the C# DatabaseHelper, note:

1. **sp_CreateAppointment** returns error messages directly - handle appropriately in UI
2. **sp_CheckDoctorAvailability** returns ConflictingAppointmentID for detailed error reporting
3. All date/time handling should use DATETIME consistently
4. Status values: 'Scheduled', 'Completed', 'Cancelled', 'No-Show' (exact casing)
5. All procedures use OUTPUT parameters - map these to C# ref parameters

---

## Files Included

1. **01_HospitalNet_Schema.sql** - Complete database creation script
2. **02_Query_Documentation.sql** - Raw SQL queries and analytics
3. **Phase1_README.md** - This documentation file

---

## Next Steps

✅ **Phase 1 Complete:** Database schema and stored procedures created

⏳ **Phase 2 (Ready for Implementation):**
- C# DatabaseHelper class with ADO.NET
- AppointmentManager class
- PatientManager class
- AnalyticsManager class
- MedicalRecordManager class

⏳ **Phase 3 (WPF GUI):**
- Dashboard
- Patient Management Forms
- Appointment Scheduling UI
- Doctor Schedule Viewer
- Medical Records Entry
- Analytics Reports

---

## Testing Recommendations

Before moving to Phase 2, test:

1. Run the schema creation script successfully
2. Insert test data (patients, doctors)
3. Test sp_CheckDoctorAvailability with overlapping times
4. Test sp_CreateAppointment with invalid scenarios
5. Test double-booking prevention (attempt to create conflicting appointment)
6. Test sp_RecordMedicalVisit workflow
7. Run all analytics queries to verify output
