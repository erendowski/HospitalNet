# 🏥 HospitalNet Phase 3 - Step 2 Complete

## Functional Views: Patients & Appointments

**Date:** December 6, 2025  
**Status:** ✅ COMPLETE - Full CRUD Operations & Double-Booking Prevention

---

## 📋 Deliverables (Phase 3 Step 2)

### 1. PatientsView.xaml & .cs
**Location:** `Frontend/Views/PatientsView.xaml` and `.xaml.cs`

#### UI Components
- ✅ **Header Section** - "Patient Management" title and description
- ✅ **Search Bar** - Real-time patient search by name, phone, or email (300px TextBox)
- ✅ **Add Patient Button** - Opens AddPatientDialog to register new patient
- ✅ **DataGrid** - Displays all active patients with 9 columns
- ✅ **Status Bar** - Shows patient count and operation status

#### DataGrid Columns
| Column | Width | Source | Format |
|--------|-------|--------|--------|
| ID | 60px | PatientID | Integer |
| First Name | 150px | FirstName | String |
| Last Name | 150px | LastName | String |
| Phone | 120px | Phone | String |
| Email | 180px | Email | String |
| Age | 60px | Age | Integer |
| DOB | 110px | DateOfBirth | MM/dd/yyyy |
| Registration Date | 130px | RegistrationDate | MM/dd/yyyy |
| Actions | 140px | Edit/Remove buttons | Template |

#### Key Features
```csharp
// Load patients from database
LoadPatients()
  → PatientManager.GetAllActivePatients()
  → Populates _allPatients and _filteredPatients collections
  → Binds to PatientsDataGrid.ItemsSource

// Real-time search filtering
SearchTextBox_TextChanged()
  → Filters _allPatients by FirstName, LastName, Phone, Email
  → Updates _filteredPatients ObservableCollection
  → Updates DataGrid in real-time as user types

// Add new patient
AddPatientButton_Click()
  → Creates AddPatientDialog instance
  → ShowDialog() → Modal dialog window
  → Reloads patients if DialogResult == true

// Edit patient
EditPatient_Click()
  → Extracts PatientID from button Tag
  → Retrieves full patient record via PatientManager.GetPatientByID()
  → Opens AddPatientDialog with patient data
  → Updates if DialogResult == true

// Delete (Deactivate) patient
DeletePatient_Click()
  → Shows confirmation dialog
  → Calls PatientManager.DeactivatePatient()
  → Reloads patient list
```

#### Data Binding
```xml
<DataGrid x:Name="PatientsDataGrid" ItemsSource="{Binding _filteredPatients}">
    <DataGridTextColumn Binding="{Binding PatientID}"/>
    <DataGridTextColumn Binding="{Binding FirstName}"/>
    ...
</DataGrid>

<!-- Action buttons -->
<Button Tag="{Binding PatientID}" Click="EditPatient_Click"/>
<Button Tag="{Binding PatientID}" Click="DeletePatient_Click"/>
```

#### Backend Integration
```csharp
PatientManager _patientManager = new PatientManager(App.ConnectionString);

// Method calls
GetAllActivePatients()        // Load all patients
GetPatientByID(patientID)     // Get specific patient
DeactivatePatient(patientID)  // Mark as inactive
UpdatePatient(patient)        // Update existing patient
AddPatient(patient)           // Add new patient (via dialog)
```

---

### 2. AppointmentsView.xaml & .cs
**Location:** `Frontend/Views/AppointmentsView.xaml` and `.xaml.cs`

#### UI Layout (3-Section Design)

##### **Section 1: Doctor & Date Selection** (Top)
```
┌─────────────────────────────────────────┐
│ Doctor: [ComboBox] | Date: [DatePicker] │
└─────────────────────────────────────────┘
```
- ComboBox for doctor selection (DisplayMemberPath="FullName")
- DatePicker for appointment date selection
- OnChange triggers appointment list refresh

##### **Section 2: Existing Appointments** (Middle)
```
┌────────────────────────────────────────────────────┐
│ Appointments for Selected Doctor & Date            │
├─────────┬────────────────┬─────────────┬───────────┤
│ Time    │ Patient        │ Reason      │ Status    │
├─────────┼────────────────┼─────────────┼───────────┤
│ 09:00   │ John Doe       │ Checkup     │ Scheduled │
│ 10:00   │ Jane Smith     │ Follow-up   │ Completed │
│ 14:30   │ Bob Johnson    │ Treatment   │ Scheduled │
└─────────┴────────────────┴─────────────┴───────────┘
```
- DataGrid showing all appointments for doctor/date combination
- Read-only display of scheduled appointments

##### **Section 3: New Appointment Booking** (Bottom)
```
┌─────────────────────────────────────────────────────┐
│ Patient: [ComboBox] | Time: [HH:mm] | Reason: [...] │
│                                     [Book Appointment]│
└─────────────────────────────────────────────────────┘
```
- Patient dropdown (populated from active patients)
- Time input (HH:mm format, e.g., "09:00")
- Reason for visit (free text)
- Book button to schedule appointment

#### Key Features

##### **Load Managers & Initial Data**
```csharp
InitializeApplication()
  ├─ Create DoctorManager, PatientManager, AppointmentManager
  ├─ LoadDoctors() → Populate DoctorComboBox
  ├─ LoadPatients() → Populate PatientComboBox
  └─ Set DatePicker.SelectedDate = Today
```

##### **Doctor Selection**
```csharp
DoctorComboBox_SelectionChanged()
  ├─ Extract selected Doctor from ComboBox
  ├─ Store in _selectedDoctor variable
  └─ Call RefreshAppointmentsList()
```

##### **Date Selection**
```csharp
DatePicker_SelectedDateChanged()
  ├─ Extract selected DateTime from DatePicker
  ├─ Store in _selectedDate variable
  └─ Call RefreshAppointmentsList()
```

##### **Refresh Appointments List**
```csharp
RefreshAppointmentsList()
  ├─ Call AppointmentManager.GetAppointmentsByDoctorAndDate()
  ├─ For each appointment:
  │  ├─ Get patient name via PatientManager.GetPatientByID()
  │  └─ Create display object with: Time, PatientName, Reason, Status
  ├─ Bind to AppointmentsDataGrid.ItemsSource
  └─ Display in read-only DataGrid
```

#### CRITICAL: Double-Booking Prevention Logic

##### **Book Button Click Handler**
```csharp
BookButton_Click()
  ├─ Step 1: Validate inputs
  │  ├─ Doctor selected?
  │  ├─ Patient selected?
  │  ├─ Date selected?
  │  ├─ Time provided in HH:mm format?
  │  └─ Show error MessageBox if validation fails
  │
  ├─ Step 2: Parse and combine date + time
  │  ├─ TimeSpan.TryParse(TimeTextBox.Text) → Validate format
  │  └─ DateTime = SelectedDate.Add(TimeSpan)
  │
  ├─ Step 3: CRITICAL TRY-CATCH BLOCK for booking
  │  │
  │  └─ TRY:
  │     ├─ Call AppointmentManager.ScheduleAppointment(
  │     │   doctorID, patientID, appointmentDateTime, reason)
  │     ├─ Show success MessageBox with Appointment ID
  │     ├─ Clear form (TimeTextBox, ReasonTextBox)
  │     └─ RefreshAppointmentsList() to show new appointment
  │
  │  └─ CATCH (Exception bookingException):
  │     │
  │     └─ *** THIS IS WHERE DOUBLE-BOOKING ERRORS ARE CAUGHT ***
  │         ├─ Exception message contains: "Doctor is not available at this time"
  │         │                               or similar double-booking error
  │         ├─ Show ERROR MessageBox with: bookingException.Message
  │         └─ User-friendly error display:
  │             "✗ Cannot book appointment:
  │             Doctor is not available at this time"
  │
  └─ User sees error and can try different time/date
```

##### **Error Handling - Double-Booking Messages**
```
Backend Exception (from AppointmentManager):
  ├─ "Doctor is not available at this time"
  ├─ "Appointment overlaps with existing booking"
  ├─ "Doctor schedule conflict detected"
  └─ Any other custom exception message

Frontend Display (MessageBox):
  ├─ MessageBox.Show(bookingException.Message, "Booking Error", ...)
  ├─ User sees: "✗ Cannot book appointment: Doctor is not available..."
  └─ User can modify time/date and try again
```

#### Backend Integration
```csharp
DoctorManager:
  ├─ GetAllDoctors()
  └─ Used to populate DoctorComboBox with FullName display

PatientManager:
  ├─ GetAllActivePatients()
  ├─ GetPatientByID(patientID)
  └─ Used to populate PatientComboBox and get patient names

AppointmentManager:
  ├─ GetAppointmentsByDoctorAndDate(doctorID, date)
  │   └─ Retrieve all appointments for doctor on specific date
  ├─ ScheduleAppointment(doctorID, patientID, dateTime, reason)
  │   └─ CRITICAL: Throws exception if double-booking detected
  └─ This is where double-booking prevention logic exists
```

---

### 3. AddPatientDialog.xaml & .cs
**Location:** `Frontend/Dialogs/AddPatientDialog.xaml` and `.xaml.cs`

#### Dialog Specification
- **Type:** Modal Window (ShowDialog())
- **Mode:** Dual (Add new | Edit existing)
- **Size:** 500x550 pixels
- **Style:** No resize, single border
- **Position:** CenterOwner (centers over parent window)

#### Form Fields (7 fields total)
```
┌─────────────────────────────────────────────────┐
│ Add New Patient                              [X]│
├─────────────────────────────────────────────────┤
│ First Name *:           [TextBox 300px]         │
│                                                 │
│ Last Name *:            [TextBox 300px]         │
│                                                 │
│ Phone *:                [TextBox 300px]         │
│                                                 │
│ Email *:                [TextBox 300px]         │
│                                                 │
│ Date of Birth *:        [DatePicker 300px]     │
│                                                 │
│ Medical History:        [TextBox 300px, 80px]  │
│                         [Multiline]             │
│                                                 │
│ Allergies:              [TextBox 300px, 60px]  │
│                         [Multiline]             │
│                                                 │
│ Status: ⚠ Error message...                      │
├─────────────────────────────────────────────────┤
│                              [Cancel] [Save]    │
└─────────────────────────────────────────────────┘
```

#### Constructor Overloads
```csharp
// Constructor 1: Add new patient
public AddPatientDialog()
{
    _isEditMode = false;
    TitleTextBlock.Text = "Add New Patient";
    Form is empty, ready for user input
}

// Constructor 2: Edit existing patient
public AddPatientDialog(Patient patientToEdit)
{
    _isEditMode = true;
    _editingPatient = patientToEdit;
    TitleTextBlock.Text = "Edit Patient";
    PopulateFields() → Fill form with patient data
}
```

#### Validation Logic
```csharp
SaveButton_Click()
  ├─ FirstName: Required, not empty
  ├─ LastName: Required, not empty
  ├─ Phone: Required, not empty
  ├─ Email: Required, not empty
  ├─ DateOfBirth: Required, must be valid date
  ├─ DateOfBirth: Must not be in future
  ├─ MedicalHistory: Optional (can be empty)
  ├─ Allergies: Optional (can be empty)
  │
  └─ If validation fails:
     ├─ Show warning in StatusTextBlock
     ├─ Set focus to invalid field
     └─ Stay in dialog for user correction
```

#### Save Operations
```csharp
// ADD MODE
if (!_isEditMode)
{
    var newPatient = new Patient {
        FirstName = FirstNameTextBox.Text,
        LastName = LastNameTextBox.Text,
        Phone = PhoneTextBox.Text,
        Email = EmailTextBox.Text,
        DateOfBirth = DateOfBirthPicker.SelectedDate.Value,
        MedicalHistory = MedicalHistoryTextBox.Text,
        Allergies = AllergiesTextBox.Text,
        RegistrationDate = DateTime.Now,
        IsActive = true
    };
    
    _patientManager.AddPatient(newPatient);
}

// EDIT MODE
if (_isEditMode)
{
    _editingPatient.FirstName = FirstNameTextBox.Text;
    _editingPatient.LastName = LastNameTextBox.Text;
    ... (update all fields)
    
    _patientManager.UpdatePatient(_editingPatient);
}

// Close dialog with success
DialogResult = true;
Close();
```

#### Error Handling
```csharp
try
{
    SaveButton_Click() → Validation & Save
}
catch (Exception ex)
{
    StatusTextBlock.Text = $"✗ Error: {ex.Message}";
    MessageBox.Show(
        $"Failed to save patient:\n{ex.Message}",
        "Save Error",
        MessageBoxButton.OK,
        MessageBoxImage.Error);
}
```

---

## 🏗️ File Structure

```
Frontend/
├── Views/
│   ├── PatientsView.xaml          (NEW - DataGrid with search)
│   ├── PatientsView.xaml.cs       (NEW - CRUD operations)
│   ├── AppointmentsView.xaml      (UPDATED - Scheduler UI)
│   └── AppointmentsView.xaml.cs   (UPDATED - Double-booking logic)
│
├── Dialogs/
│   ├── AddPatientDialog.xaml      (NEW - Add/Edit patient form)
│   └── AddPatientDialog.xaml.cs   (NEW - Form save logic)
│
├── Views/ (existing)
├── ViewModels/ (empty)
└── Resources/ (empty)
```

---

## 🔄 Data Flow

### PatientsView Flow
```
Application Start
  ↓
PatientsView Constructor
  ├─ InitializeComponent()
  └─ LoadPatients()
      ├─ PatientManager.GetAllActivePatients()
      ├─ Create _allPatients ObservableCollection
      ├─ Create _filteredPatients ObservableCollection
      └─ Bind to DataGrid

User Types in Search
  ↓
SearchTextBox_TextChanged()
  ├─ Get search text (case-insensitive)
  ├─ Filter _allPatients by FirstName, LastName, Phone, Email
  └─ Update _filteredPatients → DataGrid auto-refreshes

User Clicks "Add Patient"
  ↓
AddPatientButton_Click()
  ├─ Create AddPatientDialog()
  ├─ ShowDialog() → Modal window
  └─ If DialogResult == true:
      └─ LoadPatients() → Refresh list

User Clicks Edit/Delete
  ↓
EditPatient_Click() / DeletePatient_Click()
  ├─ Extract PatientID from button.Tag
  ├─ Get/Delete patient
  └─ LoadPatients() → Refresh list
```

### AppointmentsView Flow
```
Application Start
  ↓
AppointmentsView Constructor
  ├─ InitializeManagers()
  ├─ LoadDoctors() → Populate DoctorComboBox
  ├─ LoadPatients() → Populate PatientComboBox
  └─ DatePicker.SelectedDate = DateTime.Today

User Selects Doctor
  ↓
DoctorComboBox_SelectionChanged()
  ├─ Get selected doctor
  └─ RefreshAppointmentsList()
      ├─ Get appointments for doctor + date
      └─ Bind to DataGrid

User Selects Date
  ↓
DatePicker_SelectedDateChanged()
  ├─ Get selected date
  └─ RefreshAppointmentsList()

User Fills Booking Form & Clicks "Book"
  ↓
BookButton_Click()
  ├─ Validate all inputs
  ├─ Parse time (HH:mm format)
  ├─ Combine date + time
  │
  └─ TRY:
      ├─ AppointmentManager.ScheduleAppointment()
      ├─ Success → Show confirmation
      ├─ Clear form
      └─ RefreshAppointmentsList()
     
     CATCH (Exception):
      ├─ Show error MessageBox with exception message
      ├─ Error contains: "Doctor is not available..."
      └─ User can retry with different time
```

---

## 🎯 Double-Booking Prevention (CRITICAL)

### Architecture Overview
```
Frontend (This Layer)
  ├─ BookButton_Click() [THIS FILE]
  ├─ try-catch wraps ScheduleAppointment() call
  ├─ catch block receives exception from backend
  └─ Shows MessageBox.Show(ex.Message) to user

    ↓
    ↓ (Network Call)
    ↓

Backend (Database Layer - Phase 2)
  ├─ AppointmentManager.ScheduleAppointment()
  ├─ Check doctor availability (OUTPUT parameter)
  ├─ Validate no overlapping appointments
  ├─ If conflict → throw Exception("Doctor is not available...")
  └─ If success → Insert and return AppointmentID

    ↓
    ↓ (Database)
    ↓

MSSQL Database
  ├─ sp_ScheduleAppointment stored procedure
  ├─ Check appointment conflicts
  ├─ OUTPUT @IsAvailable = 0 (if conflict)
  ├─ Block INSERT if conflict detected
  └─ Ensure only valid appointments saved
```

### Exception Catching (Frontend Implementation)
```csharp
private void BookButton_Click(object sender, RoutedEventArgs e)
{
    try
    {
        // ... validation code ...
        
        try  // ← CRITICAL INNER TRY-CATCH
        {
            // Call backend manager - may throw exception
            int appointmentID = _appointmentManager.ScheduleAppointment(
                _selectedDoctor.DoctorID,
                patientID,
                appointmentDateTime,
                reason);
            
            // Success path
            MessageBox.Show(
                $"✓ Appointment booked successfully!\nAppointment ID: {appointmentID}",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception bookingException)  // ← CATCHES DOUBLE-BOOKING ERROR
        {
            // This is where "Doctor is not available" error is caught
            MessageBox.Show(
                $"✗ Cannot book appointment:\n{bookingException.Message}",
                "Booking Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            // User sees error and can try different time/date
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show(
            $"Unexpected error:\n{ex.Message}",
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
```

### User Experience with Double-Booking Prevention
```
Scenario: User tries to book overlapping appointment

Step 1: User selects doctor "Dr. Smith", date "Dec 6", time "10:00"
Step 2: User selects patient "John Doe", enters reason "Checkup"
Step 3: User clicks "Book Appointment" button

Step 4 (Backend Processing):
  - Backend checks if Dr. Smith is available at 10:00 on Dec 6
  - If already have appointment at 10:00 → Conflict!
  - Throw Exception("Doctor is not available at this time")

Step 5 (Frontend Error Display):
  - catch block receives exception
  - Shows MessageBox:
    "✗ Cannot book appointment:
     Doctor is not available at this time"

Step 6 (User Action):
  - User reads error message
  - Understands doctor is not available at that time
  - Tries different time (e.g., 11:00) and clicks Book again
  - If successful → Appointment created
```

---

## 📊 Data Binding & Collections

### PatientsView
```csharp
// ObservableCollection for automatic UI updates
private ObservableCollection<Patient> _allPatients;      // All patients
private ObservableCollection<Patient> _filteredPatients; // Search results

// When search changes:
_filteredPatients.Clear();
foreach (var patient in filtered)
{
    _filteredPatients.Add(patient);  // ← Triggers DataGrid update
}

PatientsDataGrid.ItemsSource = _filteredPatients;  // Binding
```

### AppointmentsView
```csharp
// Collections for UI binding
ObservableCollection<Doctor> doctors;      // DoctorComboBox
ObservableCollection<Patient> patients;    // PatientComboBox
ObservableCollection<dynamic> appointments;// AppointmentsDataGrid

DoctorComboBox.ItemsSource = new ObservableCollection<Doctor>(doctors);
PatientComboBox.ItemsSource = new ObservableCollection<Patient>(patients);
AppointmentsDataGrid.ItemsSource = appointments;
```

---

## ✅ Complete Implementation Checklist

**PatientsView:**
- [x] Header with title and description
- [x] Search TextBox with real-time filtering
- [x] "Add Patient" button → Opens AddPatientDialog
- [x] DataGrid with 9 columns (ID, FirstName, LastName, Phone, Email, Age, DOB, RegDate, Actions)
- [x] Edit button in DataGrid → Opens AddPatientDialog with patient data
- [x] Delete button in DataGrid → Confirms and deactivates patient
- [x] Status bar showing patient count
- [x] LoadPatients() on initialization
- [x] SearchTextBox_TextChanged() for filtering
- [x] AddPatientButton_Click() for new patient
- [x] EditPatient_Click() for editing
- [x] DeletePatient_Click() for deletion
- [x] Error handling with MessageBox

**AppointmentsView:**
- [x] Doctor ComboBox (top) - Populated from DoctorManager
- [x] Date DatePicker (top) - Default to today
- [x] Appointments DataGrid (middle) - Shows doctor/date appointments
- [x] Patient ComboBox (bottom) - Populated from PatientManager
- [x] Time TextBox (bottom) - HH:mm format
- [x] Reason TextBox (bottom) - Free text
- [x] Book Button (bottom) - Triggers booking
- [x] RefreshAppointmentsList() on doctor/date change
- [x] **CRITICAL:** Try-catch around ScheduleAppointment()
- [x] **CRITICAL:** MessageBox.Show(ex.Message) for double-booking error
- [x] Form clearing after successful booking
- [x] Input validation (doctor, patient, date, time)
- [x] Time format validation (HH:mm)
- [x] Error handling for all operations

**AddPatientDialog:**
- [x] Modal window (ShowDialog)
- [x] Two modes: Add new | Edit existing
- [x] Title changes based on mode
- [x] Form fields: FirstName, LastName, Phone, Email, DOB, MedicalHistory, Allergies
- [x] Validation: Required fields (FirstName, LastName, Phone, Email, DOB)
- [x] Validation: DOB cannot be in future
- [x] Save button → PatientManager.AddPatient() or UpdatePatient()
- [x] Cancel button → Close dialog without saving
- [x] Status TextBlock for error messages
- [x] DialogResult = true on success
- [x] Error handling with try-catch

---

## 🚀 Usage Instructions

### Adding a Patient
```
1. Click "Add Patient" button on PatientsView
2. Dialog opens (empty form)
3. Fill in required fields (marked with *)
4. Optional: Add medical history and allergies
5. Click "Save" button
6. Dialog closes, patient list refreshes with new patient
```

### Searching Patients
```
1. Type in search box (e.g., "john", "555-1234", "john@email.com")
2. DataGrid filters in real-time
3. Shows only matching patients
4. Clear search box to see all patients again
```

### Editing Patient
```
1. Find patient in DataGrid
2. Click "Edit" button in Actions column
3. Dialog opens with patient data pre-filled
4. Modify fields as needed
5. Click "Save" button
6. Dialog closes, patient list refreshes with updated data
```

### Deleting Patient
```
1. Find patient in DataGrid
2. Click "Remove" button in Actions column
3. Confirmation dialog appears
4. Click "Yes" to deactivate
5. Patient is marked inactive, removed from active list
```

### Scheduling Appointment
```
1. Navigate to Appointments view
2. Select doctor from dropdown
3. Select date from calendar
4. View existing appointments for that doctor/date in grid
5. Select patient from dropdown
6. Enter time (e.g., "14:30")
7. Enter reason for visit
8. Click "Book Appointment"
   - If time is available: Success message, appointment created
   - If doctor unavailable: Error message "Doctor is not available at this time"
9. On error: Try different time and click Book again
```

---

## 💡 Key Implementation Notes

1. **Connection String:** All managers use `App.ConnectionString` static property
2. **ObservableCollection:** Used for automatic UI updates when data changes
3. **Real-time Search:** Search happens as user types without button click
4. **Modal Dialogs:** AddPatientDialog uses ShowDialog() for modal behavior
5. **Error Messages:** All errors shown via MessageBox.Show() to user
6. **Double-Booking:** Exception thrown by backend, caught and displayed in UI
7. **DataGrid Binding:** Uses XAML binding to display backend data
8. **View Caching:** Views are cached in MainWindow for performance

---

## 📝 Code Quality

- ✅ XML documentation comments on all public methods
- ✅ Try-catch blocks for error handling
- ✅ Input validation before database operations
- ✅ User-friendly error messages
- ✅ Async-ready structure (can add async/await later)
- ✅ Follows WPF MVVM patterns (ready for ViewModels)
- ✅ Separation of concerns (UI logic in code-behind)
- ✅ CRITICAL: Double-booking prevention with visible error feedback

---

## 🎯 Summary

**Phase 3 Step 2 - COMPLETE ✅**

Three major components fully implemented and integrated:

1. **PatientsView** - Full CRUD patient management with search
2. **AppointmentsView** - Scheduler with critical double-booking prevention
3. **AddPatientDialog** - Reusable add/edit patient dialog

All components use backend managers from Phase 2 and display errors to users. Double-booking prevention is implemented with try-catch blocks that capture backend exceptions and show them as MessageBox errors.

---

*HospitalNet - Phase 3 Step 2*  
*Functional Views: Patients & Appointments*  
*December 6, 2025*
