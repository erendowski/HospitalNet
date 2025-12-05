# HospitalNet - Phase 1 Quick Reference Guide

## 🎯 Core Entities

### Patients Table
| Column | Type | Key | Purpose |
|--------|------|-----|---------|
| PatientID | INT | PK | Unique patient identifier |
| FirstName | NVARCHAR(100) | - | Patient's first name |
| LastName | NVARCHAR(100) | - | Patient's last name |
| DateOfBirth | DATE | - | Age calculation |
| Gender | NVARCHAR(10) | - | M/F/Other (CHECK constraint) |
| PhoneNumber | NVARCHAR(15) | - | Contact (indexed) |
| Email | NVARCHAR(100) | - | Contact (indexed) |
| IsActive | BIT | - | Soft delete (0=inactive) |
| LastVisitDate | DATE | - | Last appointment date |

### Doctors Table
| Column | Type | Key | Purpose |
|--------|------|-----|---------|
| DoctorID | INT | PK | Unique doctor identifier |
| FirstName | NVARCHAR(100) | - | Doctor's first name |
| LastName | NVARCHAR(100) | - | Doctor's last name |
| Specialization | NVARCHAR(100) | - | Medical specialty (indexed) |
| LicenseNumber | NVARCHAR(50) | UQ | Medical license (UNIQUE) |
| Email | NVARCHAR(100) | UQ | Contact email (UNIQUE) |
| YearsOfExperience | INT | - | Years in practice |
| MaxPatientCapacityPerDay | INT | - | Daily patient limit |
| IsActive | BIT | - | Active status |

### Appointments Table ⭐ CRITICAL
| Column | Type | Key | Purpose |
|--------|------|-----|---------|
| AppointmentID | INT | PK | Unique appointment ID |
| PatientID | INT | FK | Links to Patients |
| DoctorID | INT | FK | Links to Doctors |
| AppointmentDateTime | DATETIME | UQ (with DoctorID) | **DOUBLE-BOOKING PREVENTION** |
| DurationMinutes | INT | - | Appointment length |
| ReasonForVisit | NVARCHAR(500) | - | Chief complaint |
| Status | NVARCHAR(50) | - | 'Scheduled'/'Completed'/'Cancelled'/'No-Show' |
| CancellationReason | NVARCHAR(500) | - | Why cancelled |

**CRITICAL CONSTRAINT:** `UNIQUE (DoctorID, AppointmentDateTime)`

### MedicalRecords Table
| Column | Type | Key | Purpose |
|--------|------|-----|---------|
| RecordID | INT | PK | Unique record ID |
| AppointmentID | INT | FK | Links to Appointments |
| PatientID | INT | FK | Links to Patients |
| DoctorID | INT | FK | Links to Doctors |
| VisitDate | DATE | - | Visit date |
| ClinicalNotes | NVARCHAR(MAX) | - | Doctor's observations |
| Diagnosis | NVARCHAR(500) | - | Medical diagnosis |
| **PrescriptionText** | NVARCHAR(MAX) | - | **PRESCRIPTION DETAILS** |
| VitalSigns | NVARCHAR(500) | - | BP, HR, Temp, etc. |
| FollowUpRequired | BIT | - | Follow-up needed? |
| FollowUpDate | DATE | - | When to follow up |

---

## 🔐 Double-Booking Prevention

### Method 1: UNIQUE Constraint
```sql
CONSTRAINT UQ_DoctorTimeSlot UNIQUE (DoctorID, AppointmentDateTime)
```
- Prevents identical timestamps
- Database-level enforcement
- Instant UNIQUE CONSTRAINT violation if breached

### Method 2: Overlap Detection Query
```sql
-- Check if time slot overlaps with existing appointment
SELECT TOP 1 AppointmentID 
FROM Appointments a
WHERE a.DoctorID = @DoctorID
  AND a.Status NOT IN ('Cancelled', 'No-Show')
  -- Existing End > Requested Start
  AND DATEADD(MINUTE, a.DurationMinutes, a.AppointmentDateTime) > @AppointmentDateTime
  -- Existing Start < Requested End
  AND a.AppointmentDateTime < DATEADD(MINUTE, @DurationMinutes, @AppointmentDateTime);
```

### Method 3: Stored Procedure
```sql
EXEC sp_CheckDoctorAvailability 
    @DoctorID = 1,
    @AppointmentDateTime = '2025-12-15 10:00:00',
    @DurationMinutes = 30,
    @IsAvailable = @IsAvailable OUTPUT,
    @ConflictingAppointmentID = @ConflictingAppointmentID OUTPUT;
```

### Method 4: Status Filtering
Cancelled and No-Show appointments don't block availability:
```sql
WHERE a.Status NOT IN ('Cancelled', 'No-Show')
```

---

## 📋 Stored Procedures Quick Reference

### 1. sp_CheckDoctorAvailability
**Purpose:** Check if a doctor is available at a specific time
```sql
EXEC sp_CheckDoctorAvailability 
    @DoctorID = 1,
    @AppointmentDateTime = '2025-12-15 10:00:00',
    @DurationMinutes = 30,
    @IsAvailable = @IsAvailable OUTPUT,
    @ConflictingAppointmentID = @ConflictingAppointmentID OUTPUT;

-- Returns: @IsAvailable = 1 (available) or 0 (booked)
```

### 2. sp_CreateAppointment
**Purpose:** Create appointment with full validation
```sql
EXEC sp_CreateAppointment
    @PatientID = 1,
    @DoctorID = 1,
    @AppointmentDateTime = '2025-12-15 10:00:00',
    @DurationMinutes = 30,
    @ReasonForVisit = 'Hypertension checkup',
    @Notes = 'Patient requested morning appointment',
    @AppointmentID = @NewAppointmentID OUTPUT,
    @ErrorMessage = @Error OUTPUT;

-- Returns: AppointmentID or error message
```

### 3. sp_GetDoctorSchedule
**Purpose:** Get doctor's appointments for a date range
```sql
EXEC sp_GetDoctorSchedule
    @DoctorID = 1,
    @StartDate = '2025-12-01',
    @EndDate = '2025-12-31';

-- Returns: All scheduled appointments (excluding Cancelled/No-Show)
```

### 4. sp_GetPatientVisitHistory
**Purpose:** Get patient's appointment and medical record history
```sql
EXEC sp_GetPatientVisitHistory @PatientID = 1;

-- Returns: All appointments and medical records for patient
```

### 5. sp_RecordMedicalVisit
**Purpose:** Record clinical visit after appointment completion
```sql
EXEC sp_RecordMedicalVisit
    @AppointmentID = 5,
    @ClinicalNotes = 'Patient stable, continues medication',
    @Diagnosis = 'Essential Hypertension - Controlled',
    @PrescriptionText = 'Lisinopril 10mg daily, Amlodipine 5mg daily',
    @VitalSigns = 'BP: 130/85, HR: 72, Temp: 98.6',
    @FollowUpRequired = 1,
    @FollowUpDate = '2026-01-15',
    @RecordID = @NewRecordID OUTPUT,
    @ErrorMessage = @Error OUTPUT;

-- Side effects: Updates appointment status, creates medical record
```

### 6. sp_CancelAppointment
**Purpose:** Cancel a scheduled appointment
```sql
EXEC sp_CancelAppointment
    @AppointmentID = 5,
    @CancellationReason = 'Patient requested reschedule',
    @ErrorMessage = @Error OUTPUT;

-- Side effects: Updates appointment status and cancellation info
```

---

## 📊 Analytics Queries Summary

| Query | Purpose | Key Metric |
|-------|---------|-----------|
| Cancellation Rate | % appointments cancelled per doctor | CancellationRatePercent |
| Patient Load | Daily patient count and hours | TotalMinutesBooked |
| Completion Rate | % scheduled appointments completed | CompletionRatePercent |
| Medical Records | Diagnoses and prescriptions | Diagnosis, PrescriptionText |
| Workload Analysis | Doctor hours per day | HoursWorked |
| Patient Frequency | Repeat patient analysis | TotalAppointments |

---

## 🚀 Workflow Examples

### Scenario 1: Schedule a New Appointment
1. Check doctor availability: `sp_CheckDoctorAvailability`
2. If available: Create appointment: `sp_CreateAppointment`
3. If booked: Return error, suggest alternative times

### Scenario 2: Complete an Appointment
1. Doctor enters clinical information
2. Call `sp_RecordMedicalVisit` with:
   - Clinical Notes
   - Diagnosis
   - **Prescription Text**
   - Vital Signs
   - Follow-up date (if needed)
3. Appointment status → 'Completed'
4. Medical record created

### Scenario 3: View Doctor's Schedule
1. Call `sp_GetDoctorSchedule` with date range
2. Display chronologically sorted appointments
3. Show patient names, duration, reason, status

### Scenario 4: Cancel Appointment
1. Verify appointment exists and is in 'Scheduled' status
2. Call `sp_CancelAppointment` with cancellation reason
3. Time slot becomes available for rebooking

---

## 📁 File Descriptions

| File | Contains |
|------|----------|
| `01_HospitalNet_Schema.sql` | Database creation, tables, procedures |
| `02_Query_Documentation.sql` | Raw SQL queries, analytics, utilities |
| `03_SampleData.sql` | Test data: 8 patients, 6 doctors, 18 appointments |
| `Phase1_README.md` | Detailed documentation and design rationale |
| `Phase1_DeliverySummary.md` | Project overview and Phase 2 preparation |
| `Phase1_QuickReference.md` | This file - quick lookup guide |

---

## 🔄 Status Values

| Status | Meaning | Can Transition To | Blocks Time Slot |
|--------|---------|-------------------|------------------|
| **Scheduled** | Appointment booked | Completed, Cancelled, No-Show | YES ✓ |
| **Completed** | Visit happened | - | NO ✗ |
| **Cancelled** | Appointment cancelled | - | NO ✗ |
| **No-Show** | Patient didn't show up | - | NO ✗ |

---

## 🔒 Data Validation

### Constraints Enforced
- `Gender IN ('Male', 'Female', 'Other')`
- `Status IN ('Scheduled', 'Completed', 'Cancelled', 'No-Show')`
- `DurationMinutes > 0`
- `YearsOfExperience >= 0`

### Unique Fields
- `Doctors.LicenseNumber` - Medical license must be unique
- `Doctors.Email` - Email must be unique
- `Appointments` - Same doctor cannot have two appointments at same DateTime

### Foreign Key Constraints
- Deleting a Patient → Cascades to Appointments and MedicalRecords
- Deleting a Doctor → Restricted (must reassign appointments first)
- Deleting an Appointment → Cascades to MedicalRecords

---

## 💡 Key Design Decisions

1. **No ORM** - Raw SQL queries for full control and performance
2. **UNIQUE Constraint** - First line of defense against double-booking
3. **Overlap Detection** - Handles appointments of different durations
4. **Status Filtering** - Cancelled/No-Show don't block time slots
5. **Soft Deletes** - IsActive flags preserve historical data
6. **Denormalization** - PatientID/DoctorID in MedicalRecords for query performance
7. **Audit Trail** - CreatedDate/UpdatedDate on all tables
8. **Specialization Indexing** - Fast lookup of doctors by specialty

---

## ⚡ Performance Tips

1. Always include Status filter in queries: `WHERE Status NOT IN ('Cancelled', 'No-Show')`
2. Use indexes on DoctorID and AppointmentDateTime
3. Filtered index on FollowUpRequired for follow-up tracking
4. Avoid SELECT * - specify needed columns
5. Use TOP 1 for existence checks instead of COUNT

---

## 🧪 Quick Test

```sql
-- Test double-booking prevention
DECLARE @IsAvailable BIT, @ConflictingID INT;

-- Check if Dr. Anderson (ID=1) is available Dec 15 at 10:00
EXEC sp_CheckDoctorAvailability 
    @DoctorID = 1,
    @AppointmentDateTime = '2025-12-15 10:00:00',
    @DurationMinutes = 30,
    @IsAvailable = @IsAvailable OUTPUT,
    @ConflictingAppointmentID = @ConflictingID OUTPUT;

SELECT @IsAvailable AS Available, @ConflictingID AS ConflictingAppointmentID;
```

---

**Ready to move to Phase 2? Confirm and we'll build the C# backend!**
