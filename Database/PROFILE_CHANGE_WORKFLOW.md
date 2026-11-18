# Profile update HR approval

Self-service changes on **My Profile** (own employee record, non-admin) do not overwrite official `tblEmployee` / contact / address / identification data until an HR Manager approves.

## Flow

1. Employee edits personal, phone/email contacts, address, or CNIC and saves.
2. Confirmation page shows current approved vs proposed values.
3. Submit creates `PCR-#####` with status **Pending HR Approval**. Official profile stays unchanged.
4. Bell icon + inbox notify the employee and HR reviewers (database only, no email).
5. HR opens **Profile Requests**, compares values, Approve or Reject (reject requires comments).
6. Approve applies the whole request in one transaction. Reject keeps previous approved values.
7. Employee is notified in-app. History remains on **My profile requests**.

Admins editing any record, and HR editing **someone else**, still save directly to the official profile.

An employee cannot approve their own request.

## Who can review

System admin, or User Rights: **Profile Update Requests** (read/approve), **Employee Master** approve/write, or **Leave Management - HR Manager** read.

## If SQL login cannot CREATE TABLE

Run `Database/Production_ProfileChange_Patch.sql`.

## Test

A. Change mobile on My Profile → confirm → pending → official number unchanged → HR approve → new number on profile.  
B. Change CNIC/address incorrectly → HR reject with reason → official values unchanged → employee sees reason in inbox/history.  
C. Change phone, email, address, CNIC in one submit → one request → approve updates all or reject updates none.
