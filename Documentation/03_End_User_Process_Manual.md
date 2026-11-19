# Ghazi Brothers HRMS — End-User Process Manual

**Document type:** End-User Process Manual  
**Application:** Ghazi Brothers Human Resource Management System (HRMS)  
**Audience:** Employees, managers, HR users, management approvers  
**Scope:** How to use the existing application (standalone documentation; not part of the application)

---

## 1. Getting Started

### 1.1 What you need

- A browser  
- Your **HRMS username and password** (issued by IT/HR admin)  
- Preferably, your user account **linked to your employee profile** (required for many self-service features)
- If two-factor authentication is enabled, an authenticator app (Microsoft/Google Authenticator or similar) used **offline**

### 1.2 First-time tips

- If you see a warning that your account is **not linked to an employee profile**, contact HR/IT. Self-service items (profile, documents, personal leave/requests) may be limited until linkage is fixed.  
- Use the **Home** page tiles for the features you are allowed to open. Missing tiles usually mean missing permissions—not a system outage.  
- Use **Account** in the header for password and 2FA. Use **My Profile** for official employee data (those changes wait for HR).  
- The **bell** icon is the in-app inbox. HRMS does not send email for profile requests or password recovery.

---

## 2. Login and Navigation

### 2.1 Login

1. Open the HRMS website URL provided by your organization.  
2. On the login screen, enter **Username** and **Password**.  
3. Submit the form (Login).  
4. If the administrator marked **must change password**, complete **Account Settings** before other pages.  
5. If **two-factor authentication** is on, enter the 6-digit code from your authenticator app, or an unused **backup code**.  
6. On success, you arrive at **Home**.

**Expected result:** Home shows a gallery (if configured) and process tiles you are allowed to use.

**If login fails:**

| Symptom | What to try |
|---------|-------------|
| Invalid credentials | Recheck username/password; Caps Lock |
| Repeated failures | Wait (lockout window, typically 15 minutes) or ask admin to **Clear lockout** on User Setup |
| Authenticator code rejected | Check phone time; wait for the next 30-second code; or use a backup code |
| Account inactive | Ask administrator to reactivate `IsActive` |
| Unexpected error | Ask IT to check database connectivity |

### 2.1.1 Forgot password (no email)

1. On the login screen, open **Forgot password**.  
2. Enter your username.  
3. Answer your recovery questions **or** enter an unused backup code.  
4. Set a new password that meets the policy.

The site never emails a reset link. If you have no questions and no unused backup codes, ask an administrator to reset your password (you must change it at next sign-in).

### 2.2 Home page

Home groups tiles such as:

| Category | Examples |
|----------|----------|
| Self Service | My Profile, Account Settings, My Documents, Knowledge Library |
| Communication | Announcements, Memorandums |
| HR Process | Leave, Attendance, Requests, Profile Update Requests, Discipline, Performance, Training, Expenses, Recruitment, Directory |
| Master Data | Employee Master, Position Master |
| Shortcuts | Quick Links |

Click a tile to open that feature.

### 2.3 Navigation tips

- Use the application **header** (where available) to return Home, open **Account**, or open the **bell** (inbox).  
- After finishing a task, return to Home to pick another process.  
- If you open a URL you cannot access, you may be redirected with an **access denied** indication—request rights from your administrator.

### 2.4 Session

Your session remains active while you use the system. After a long idle period you may need to log in again. Changing your password or 2FA signs other browsers out on their next request.

### 2.5 Account Settings (password, recovery, 2FA)

**Purpose:** Manage your **user account**. This is not the official employee profile.

1. After sign-in, open **Account** in the header (or Account Settings).  
2. **Display name:** update if allowed; username cannot be changed here.  
3. **Change password:** enter current password, then a new password that meets the policy.  
4. **Recovery questions:** set two questions (current password required). You will need these if you forget the password.  
5. **Two-factor authentication:**  
   - Enter current password.  
   - Add the setup key (or QR / `otpauth` URI) in Microsoft Authenticator, Google Authenticator, or another TOTP app. Airplane mode is fine; no internet is required.  
   - Confirm with a 6-digit code.  
   - **Save the backup codes** immediately; each code works once.  
6. To turn 2FA off, use Account Settings with your current password, or ask an administrator (**User Setup → Disable 2FA**) if you lost the phone and have no backup codes.

**Expected result:** Next login asks for the authenticator code when 2FA is enabled. Lost phone: backup code, recovery questions, or admin disable 2FA plus password reset.

---

## 3. My Profile (official employee data — HR approval)

### 3.1 Purpose

View your **approved** official employee profile. When you change fields you are allowed to edit, the values go to an **HR Manager** for approval. The official profile does **not** change until HR approves. If HR rejects, every field stays as it was.

This applies whether you change **one field** or **many fields** in the same save (for example phone, email, residential address, and CNIC together).

HR staff who edit **someone else’s** record in Employee Master still save immediately. If they edit **their own** linked record, they also go through this workflow and cannot approve themselves.

### 3.2 What you can change (typical self-service)

Personal details such as name, CNIC / national ID, date of birth, gender, nationality, religion, marital status, domicile, blood group, contacts (phone/email), addresses, and identification documents—plus other profile tabs you can still edit.

Fields locked for profile-only users (salary, work agreement, attendance method, department, designation, joining dates, and similar) stay under HR control.

### 3.3 Employee: submit a change

1. From Home, open **My Profile** (or Employee Master on your own record).  
2. You see the current **approved** official values.  
3. Change one or more allowed fields (or contact / address / identification tabs).  
4. Save.  
5. On **Confirm profile update**, check **Current approved value** vs **Proposed value** for every field.  
6. **Submit to HR** (or Cancel to discard the draft).  
7. Official values on My Profile stay the old ones until HR decides.

**Expected result:** Request number `PCR-#####`, status **Pending HR Approval**. Header bell / Inbox notifies you and HR. You cannot submit another profile request for the same employee until this one is approved or rejected.

### 3.4 HR: review, approve, or reject

1. Open **Profile Update Requests** (Home tile or inbox link), or `/ProfileChangeReview.aspx`.  
2. Open the pending request.  
3. Review **every** listed field (old vs proposed).  
4. **Approve** — all proposed fields in that request are written to the official profile in one step. If apply fails, nothing is half-updated.  
5. **Reject** — enter a **reason** (required). No official field is changed.

The person who submitted the request cannot approve or reject it. Another HR Manager or administrator must act.

**Expected result after approve:** Employee sees updated official values; inbox says approved.  
**Expected result after reject:** Official values unchanged; employee sees the reason in inbox / My profile requests.

### 3.5 Track your requests

Open **My profile requests** (`/ProfileChangeHistory.aspx`) or the inbox link. History shows submitted, approved, and rejected actions.

### 3.6 Inbox and bell

1. Click the **bell** in the header or open **Inbox**.  
2. Unread items include “profile update submitted / pending / approved / rejected.”  
3. Follow the link to the request. Marking related items read happens when HR decides.

No email is sent.

### 3.7 Common issues

| Issue | Cause / action |
|-------|----------------|
| Page unavailable | No rights or not linked — contact admin |
| Cannot edit | Read only — request Write on User Profile |
| “No changes were detected” | Values match the approved profile, or only locked HR fields were changed |
| Already have a pending request | Wait for HR to approve or reject `PCR-#####` |
| Cannot approve my own request | Expected — another HR user must review |
| Official values unchanged after Save | Expected until HR approves |
| Compilation error on Confirm page (production) | IT: `netstandard` in `Web.config` / `bin` (see Technical Documentation) |

---

## 4. My Documents

### 4.1 Purpose

View documents associated with your employee record.

### 4.2 Steps

1. Open **My Documents** from Home.  
2. Browse the list of documents.  
3. Open or download as provided by the screen.

**Expected result:** Your documents appear. Administrators may see broader document access.

**Note:** If the tile is missing, you likely lack access (no link / no rights).

---

## 5. Knowledge Library

### 5.1 Purpose

Access manuals, SOPs, policies, and reference materials published by the organization.

### 5.2 Steps

1. Open **Knowledge Library**.  
2. Filter or browse by category (e.g. General, Department, System Manual, SOP, Policy, Reference).  
3. Open documents you are allowed to see.

**Expected result:** Only documents within your access scope appear.

**FAQ:** “I cannot see a policy others mention.” — It may be restricted by department/job/grant. Ask the LMS publisher or HR.

---

## 6. Announcements and Memorandums

### 6.1 Announcements

1. Open **Announcements**.  
2. Read listed notifications.  
3. Open an item for full detail if available.

### 6.2 Memorandums

1. Open **Memorandums**.  
2. Review active memorandums / policy documents.  

**Expected result:** Current communications are visible according to how they were published in setup.

---

## 7. Leave Management

### 7.1 Purpose

Apply for and manage leave applications.

### 7.2 Create a leave application (typical)

1. Open **Leave Management**.  
2. Start a new leave application.  
3. Select **Leave Category** (e.g. Casual, Annual, Sick).  
4. Choose leave **type** (e.g. Planned, Unplanned, Future Unplanned Leave) as shown on the form.  
5. Enter **start date**, **end date**, and **days**.  
6. Enter **reason**.  
7. Assign **temporary / permanent responsible** employees if required by your process.  
8. Save.

**Expected result:** The leave record appears in the list for your scope.

### 7.3 Data entry tips

- Ensure dates are correct and days match the period.  
- Use the correct category; ask HR if unsure.  
- Self-service users normally work on their **own** employee record when linked.

### 7.4 Troubleshooting

| Issue | Action |
|-------|--------|
| Cannot see colleagues’ leave | Normal if your data scope is Own Only |
| Categories missing | Ask admin to configure Leave Category Setup |
| Cannot open module | Request `LeaveMaster` read/write rights |

---

## 8. Employee Attendance

### 8.1 Purpose

View attendance from the company attendance system (read-only in HRMS).

### 8.2 Steps

1. Open **Employee Attendance**.  
2. Set filters (date range, employee, department, status) as needed.  
3. Choose a view such as:  
   - Daily Attendance  
   - Monthly Summary  
   - Late Arrival  
   - Early Departure  
   - Absentee  
   - Other punch/detail views offered on the screen  
4. Review KPIs and the grid.  
5. Page through results if more than one page exists.

**Expected result:** Punch/status data for the selected period. Employee names/departments appear when Biometric ID is correctly mapped in Employee Master.

### 8.3 Validation / interpretation

- Attendance is **not edited** in HRMS—corrections are done in the source attendance system / HR process outside this screen.  
- Blank employee enrichment often means **Biometric ID** is missing or mismatched.

### 8.4 Troubleshooting

| Issue | Action |
|-------|--------|
| No data / connection error | IT must verify Attendance database connectivity |
| Wrong person name | HR: verify Biometric ID on Employee Master |
| Tile missing | Request `EmployeeAttendance` rights |

---

## 9. Employee Requests (Manager → HR → Management)

### 9.1 Purpose

Submit and track requests such as Mobile Phone, Health Claim, Travelling, Employee Loan, Vehicle, Fuel, or Other.

### 9.2 Who does what

| Role | What they do |
|------|----------------|
| Employee | Create draft, attach files, submit, revise if returned |
| Manager | Approve / reject / return at **Pending Manager Approval** |
| HR | Approve / reject / return at **Pending HR Approval** (needs HR request rights) |
| Management | Approve / reject / return at **Pending Management Approval** (needs Management request rights) |

### 9.3 Employee: create and submit

1. Open **Employee Requests**.  
2. Create a new request.  
3. Select **category**.  
4. Enter **subject**, **description**, and category-specific details (amount, dates, etc. as shown).  
5. Attach supporting files if required.  
6. Save as **Draft** if you need to finish later.  
7. When ready, **Submit**.

**Expected result after submit:** Status becomes **Pending Manager Approval**. The system assigns your manager based on job Reports-To or Permanent Responsible.

### 9.4 Approver: act on a request

1. Open **Employee Requests**.  
2. Locate items pending your stage (manager / HR / management).  
3. Open the request; review details and attachments.  
4. Choose one action:  
   - **Approve** — advances to the next stage (or completes if Management)  
   - **Reject** — ends the request (**comments required**)  
   - **Return for correction** — employee can edit and resubmit  

**Expected results:**

| Action | New status |
|--------|------------|
| Manager approve | Pending HR Approval |
| HR approve | Pending Management Approval |
| Management approve | Approved |
| Reject | Rejected |
| Return | Returned for Correction |

### 9.5 Employee: after return for correction

1. Open the request (status Returned for Correction).  
2. Update fields / attachments.  
3. Submit again → returns to Pending Manager Approval.

### 9.6 Data entry checklist

- Subject clearly states the need.  
- Amounts and dates complete for loans/travel/etc.  
- Attachments readable and complete.  
- Do not edit after submit unless status allows (Draft / Returned).

### 9.7 Troubleshooting

| Issue | Action |
|-------|--------|
| Cannot submit | Ensure required fields filled; status must be Draft or Returned |
| Stuck without manager | Ask HR to set Job Reports-To or Permanent Responsible |
| HR cannot approve | Grant `EmployeeRequestHR` rights |
| Management cannot approve | Grant `EmployeeRequestManagement` rights |
| Reject without comments | Enter comments, then reject again |

---

## 10. Discipline & Non-Conformance

### 10.1 Purpose

HR (and permitted users) manage disciplinary / non-conformance cases: investigation, findings, corrective and disciplinary actions, and closure.

### 10.2 Typical workflow

1. Open **Discipline & Non-Conformance**.  
2. Create a case for the employee; capture initial details.  
3. Move status through: **Open** → **Under Investigation** → **Action Required** → **Resolved** → **Closed** as the case progresses.  
4. Record investigation notes, findings, corrective actions, disciplinary action, responsible person, final decision, and management remarks.  
5. Add attachments as needed.  
6. Mark findings **Include in Performance** when they should appear in performance review context.  
7. Save / update status until closed.

**Expected result:** Case history and audit trail retained; performance module can show flagged findings.

### 10.3 Troubleshooting

| Issue | Action |
|-------|--------|
| Cannot open module | Request `EmployeeDiscipline` rights |
| Employee list limited | Data scope may restrict visible employees |

---

## 11. Employee Performance

### 11.1 Purpose

Record and review performance cycles. Related discipline findings marked for performance may appear for context.

### 11.2 Steps

1. Open **Employee Performance**.  
2. Select or create the performance record as the screen allows.  
3. Enter evaluation details.  
4. Review any linked discipline findings if shown.  
5. Save.

**Note:** Adding certain findings may require write rights on Discipline as well as Performance access.

---

## 12. Training, Expenses, and Recruitment

### 12.1 Employee Training

1. Open **Employee Training**.  
2. Add or update training records (course, dates, outcomes as on form).  
3. Save.

### 12.2 Expense Process

1. Open **Expense Process**.  
2. Create a claim; select categories/amounts/dates.  
3. Attach receipts if the form supports it.  
4. Save / submit per on-screen actions.

### 12.3 Recruitment Process

1. Open **Recruitment Process**.  
2. Create or update hiring/recruitment records.  
3. Save.

**Common rule:** If a tile is missing, request the matching form rights from your administrator.

---

## 13. Employee Directory

### 13.1 Steps

1. Open **Employee Directory**.  
2. Search/filter employees.  
3. View details.  
4. Export if you have export permission and the button is available.

---

## 14. Employee Master and Position Master (HR)

### 14.1 Employee Master

**Audience:** HR / administrators with Employee Master rights.

1. Open **Employee Master**.  
2. Search or create an employee.  
3. Maintain personal, job, organization, contact, address, **identification (CNIC and related)**, family, bank, education, documents, certificates.  
4. Set **Biometric ID** to match the attendance system employee code.  
5. Link `UserID` when the employee should use self-service.  
6. Save.

**Own record:** If the employee is **you**, Save starts the profile approval workflow (§3). Official values wait for another HR user.  
**Other employees:** Save writes official values immediately.

**Expected result:** Employee available to other modules; attendance enrichment works when Biometric ID matches. Grant HR **Profile Update Requests** read/approve so colleagues’ self-service changes can be reviewed.

### 14.2 Position Master / Hierarchy

1. Open **Position Master** (or Hierarchy).  
2. Create/update positions and reporting structure.  
3. Save.

**Note:** Position reporting supports org design. Request approvals use **Job Reports-To employee** / **Permanent Responsible**, which should be kept accurate for workflow routing.

---

## 15. Quick Links

1. Open **Quick Links**.  
2. Click the software or process shortcut you need.  

Links are maintained in Software Link Setup by administrators.

---

## 16. Setup Screens (Administrators)

Administrators maintain dropdowns and masters, for example:

- Organization: departments, locations, banks, cities, etc.  
- Employee: grades, jobs, leave categories, document types  
- LMS Document Setup: publish knowledge articles  
- Notification / Memorandum / Image Gallery setup  
- Software Link Setup  

**General pattern:** Open setup page → add/edit/delete (per rights) → save → verify values appear on transactional screens.

---

## 17. User and Rights Administration

### 17.1 Create a user

1. Open **User Setup**.  
2. Create username; set password per policy; mark Active.  
3. Set Admin only for trusted administrators.  
4. Save.  
5. Ensure an employee record is linked (`UserID`) for self-service.  
6. As needed: force **must change password** on reset, **Disable 2FA**, or **Clear lockout**.

### 17.2 Assign rights

1. Open **User Rights Setup**.  
2. Select the user.  
3. Grant Read/Write/Delete/Approve/Export per form.  
4. For request approvers, grant:  
   - `Employee Request - HR` for HR stage  
   - `Employee Request - Management` for management stage  
5. For HR profile reviewers, grant **Profile Update Requests** (`ProfileChange`) Read and Approve.  
6. Save.

**Expected result:** Matching Home tiles and page actions become available after next login/session refresh as applicable.

### 17.3 Audit

Use **Audit Log Report** to review security-relevant activity when investigating issues.

---

## 18. Expected System Responses (Quick Reference)

| Action | Typical response |
|--------|------------------|
| Successful login | Home with tiles (or 2FA page, or Account Settings if must-change) |
| 2FA enabled | Authenticator or backup code after password |
| No permission | Access denied / tile hidden |
| Unlinked employee | Warning on Home; limited self-service |
| Own profile Save | Confirm page → Pending HR Approval; official values unchanged |
| HR approves profile request | All fields in that request update together |
| HR rejects profile request | Official values unchanged; comments visible to employee |
| Employee request submit | Status → Pending Manager Approval |
| Final management approve | Status → Approved |
| Reject employee request | Status → Rejected (with comments) |
| Attendance open | Read-only grid from attendance DB |
| Save master data | Confirmation / refreshed record |

---

## 19. Common Errors and Troubleshooting

| Problem | Likely cause | What to do |
|---------|--------------|------------|
| Cannot log in | Wrong password / inactive user / lockout / 2FA | Reset via admin; Clear lockout; Disable 2FA; confirm Active |
| Forgot password | No email reset | Questions, backup code, or admin reset |
| Blank Home (few tiles) | Limited rights | Request needed FormKeys |
| Profile sync warning | No employee link | HR links `tblEmployee.UserID` |
| Request never reaches HR | Waiting on manager / wrong manager | Fix Reports-To or Permanent Responsible; ask manager to act |
| HR/Management buttons missing | Missing special FormKeys | Grant EmployeeRequestHR / EmployeeRequestManagement |
| Attendance names wrong | Biometric ID mismatch | Correct Employee Master Biometric ID |
| Attendance empty | DB connectivity / filters | Widen dates; IT checks AttendanceConnection |
| Upload fails | File too large | Reduce size; respect site upload limit |
| Knowledge doc missing | Scope restriction | Ask publisher to grant access |
| Own profile save did not update | Waiting for HR | Check Inbox / My profile requests |
| Cannot approve own profile request | By design | Another HR Manager must decide |
| Profile Confirm compilation error | Missing `netstandard` on IIS | See Technical Documentation §11.3.1 |
| Page error after deploy | Schema/patch missing | IT applies Production SQL patches (including Profile Change and Account Security) |

---

## 20. Frequently Asked Questions

**Q: Why don’t I see Employee Requests on Home?**  
A: You need module access (rights and/or linked employee). Ask your administrator.

**Q: I am a manager but cannot approve.**  
A: You must be the employee’s resolved manager (Job Reports-To or Permanent Responsible), and the request must be in Pending Manager Approval.

**Q: I work in HR but cannot approve requests.**  
A: Read/write on **Employee Request - HR** (`EmployeeRequestHR`) is required, not only Employee Requests.

**Q: Can I edit attendance in HRMS?**  
A: No. HRMS displays attendance from the external attendance database.

**Q: Where do I change my password?**  
A: Header **Account** → Account Settings. If you cannot sign in, use Forgot password (questions or backup code) or ask an administrator.

**Q: Why didn’t my phone/email/CNIC change after I saved My Profile?**  
A: Official values wait for HR. Open Inbox or My profile requests. Another HR user must Approve. Reject leaves previous values.

**Q: Can I change several profile fields at once?**  
A: Yes. Submit once; HR sees all proposed fields; Approve updates all of them, Reject updates none.

**Q: I am HR and I updated my own employee record. Who approves it?**  
A: You cannot approve your own request. A different HR Manager or administrator must review it.

**Q: What is Biometric ID?**  
A: The employee’s ID in the attendance/punch system. It must match for correct attendance name mapping.

**Q: Are these manuals inside the application?**  
A: No. They are standalone documents for training and operations.

---

## 21. Feature Procedure Index

| Feature | Section |
|---------|---------|
| Login / Home / 2FA / Forgot password | §2 |
| Account Settings | §2.5 |
| My Profile / HR profile approval / Inbox | §3 |
| My Documents | §4 |
| Knowledge Library | §5 |
| Announcements / Memorandums | §6 |
| Leave | §7 |
| Attendance | §8 |
| Employee Requests | §9 |
| Discipline | §10 |
| Performance | §11 |
| Training / Expenses / Recruitment | §12 |
| Directory | §13 |
| Employee / Position Master | §14 |
| Quick Links | §15 |
| Setup | §16 |
| Users & Rights | §17 |

---

## 22. Document Control

| Item | Value |
|------|--------|
| Companion documents | Functional Documentation; Technical Documentation |
| Integration | Standalone only — not embedded in the application UI |
| Updated | Login 2FA/lockout/forgot password, Account Settings, My Profile HR approval, Inbox |

---
