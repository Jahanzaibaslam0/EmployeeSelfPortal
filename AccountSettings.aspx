<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="AccountSettings.aspx.cs" Inherits="HRMS.AccountSettingsPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<input type="hidden" name="__handler" id="__handler" value="" />
<% if (!string.IsNullOrEmpty(AlertMessage)) { %>
<div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div>
<% } %>
<% if (Account == null) { %>
<div class="alert alert-error">Account not found.</div>
<% } else { %>

<div class="card">
    <div class="card-header"><h2>Account</h2></div>
    <div class="card-body">
        <p class="text-muted" style="margin-bottom:1rem;">Username and user code cannot be changed here. Employee details stay on <a href="/UserProfile.aspx">My Profile</a>.</p>
        <div class="form-grid">
            <div class="form-group"><label>Username</label><input type="text" class="form-control" value="<%= Server.HtmlEncode(Account.Username) %>" readonly /></div>
            <div class="form-group"><label>User code</label><input type="text" class="form-control" value="<%= Server.HtmlEncode(Account.UserCode) %>" readonly /></div>
            <div class="form-group"><label>Role</label><div><% if (Account.IsAdmin) { %><span class="badge badge-warning">Admin</span><% } else { %><span class="badge badge-info">User</span><% } %> <% if (Account.IsActive) { %><span class="badge badge-success">Active</span><% } %></div></div>
            <div class="form-group"><label>Two-factor</label><div><% if (Account.TwoFactorEnabled) { %><span class="badge badge-success">Enabled</span><% } else { %><span class="badge badge-warning">Off</span><% } %></div></div>
        </div>
        <div class="form-grid" style="margin-top:1rem;">
            <div class="form-group"><label>Display name *</label><input type="text" name="fullName" class="form-control" maxlength="100" value="<%= Server.HtmlEncode(Account.FullName) %>" /></div>
            <div class="form-group"><label>Email (internal directory only)</label><input type="text" name="email" class="form-control" maxlength="100" value="<%= Server.HtmlEncode(Account.Email) %>" /></div>
        </div>
        <p class="text-muted" style="font-size:.85rem;">Email is stored on this server only. Password recovery does not send mail.</p>
        <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='SaveProfile';">Save profile</button>
    </div>
</div>

<div class="card mt-4">
    <div class="card-header"><h2>Change password</h2></div>
    <div class="card-body">
        <p class="text-muted"><%= Server.HtmlEncode(HRMS.Services.PasswordHelper.PolicySummary) %></p>
        <div class="form-grid">
            <div class="form-group"><label>Current password *</label><input type="password" name="currentPassword" class="form-control" autocomplete="current-password" /></div>
            <div class="form-group"><label>New password *</label><input type="password" name="newPassword" class="form-control" autocomplete="new-password" /></div>
            <div class="form-group"><label>Confirm new password *</label><input type="password" name="confirmPassword" class="form-control" autocomplete="new-password" /></div>
        </div>
        <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='ChangePassword';">Update password</button>
    </div>
</div>

<div class="card mt-4">
    <div class="card-header"><h2>Two-factor authentication (offline TOTP)</h2></div>
    <div class="card-body">
        <p>Use Microsoft Authenticator, Google Authenticator, or any TOTP app. After you add the account, codes work with no internet. Do not use SMS or cloud MFA.</p>
        <% if (NewRecoveryCodes != null && NewRecoveryCodes.Count > 0) { %>
        <div class="alert alert-warning">
            <strong>Save these backup codes now.</strong> Each code works once if you lose your phone.
            <ul style="margin:.75rem 0 0; font-family:Consolas,monospace; columns:2;">
            <% foreach (var c in NewRecoveryCodes) { %><li><%= Server.HtmlEncode(c) %></li><% } %>
            </ul>
        </div>
        <% } %>

        <% if (!string.IsNullOrEmpty(PendingSecret)) { %>
        <div class="alert alert-info">
            <p><strong>Setup key</strong> (Authenticator &rarr; Add account &rarr; Enter a setup key, time-based):</p>
            <p style="font-family:Consolas,monospace;font-size:1.1rem;letter-spacing:.08em;"><%= Server.HtmlEncode(HRMS.Services.TotpHelper.FormatSecret(PendingSecret)) %></p>
            <p style="font-size:.8rem;word-break:break-all;">URI: <%= Server.HtmlEncode(PendingOtpAuth) %></p>
        </div>
        <div class="form-group" style="max-width:240px;">
            <label>6-digit code from the app *</label>
            <input type="text" name="totpCode" class="form-control" maxlength="8" autocomplete="one-time-code" />
        </div>
        <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Confirm2FA';">Confirm and enable</button>
        <% } else if (!Account.TwoFactorEnabled) { %>
        <div class="form-group" style="max-width:280px;">
            <label>Current password *</label>
            <input type="password" name="begin2faPassword" class="form-control" />
        </div>
        <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Begin2FA';">Start authenticator setup</button>
        <% } else { %>
        <p>Backup codes remaining: <strong><%= Account.UnusedRecoveryCodes %></strong>
           <% if (Account.TwoFactorConfirmedOn.HasValue) { %> &middot; Enabled <%= Account.TwoFactorConfirmedOn.Value.ToString("dd-MMM-yyyy") %><% } %></p>
        <div class="form-grid">
            <div class="form-group"><label>Current password (new backup codes)</label><input type="password" name="regenPassword" class="form-control" /></div>
        </div>
        <button type="submit" class="btn btn-secondary" onclick="document.getElementById('__handler').value='RegenCodes';">Generate new backup codes</button>
        <hr style="margin:1.25rem 0;" />
        <div class="form-grid">
            <div class="form-group"><label>Current password (disable 2FA)</label><input type="password" name="disable2faPassword" class="form-control" /></div>
            <div class="form-group"><label>Authenticator or backup code</label><input type="text" name="totpOrRecovery" class="form-control" /></div>
        </div>
        <button type="submit" class="btn btn-secondary" onclick="if(!confirm('Turn off two-factor authentication?')) return false; document.getElementById('__handler').value='Disable2FA';">Disable 2FA</button>
        <% } %>
    </div>
</div>

<div class="card mt-4">
    <div class="card-header"><h2>Password recovery questions</h2></div>
    <div class="card-body">
        <p class="text-muted">Used on this internal server only when you forget your password. Answers are stored hashed. <%= Account.SecurityQuestionCount >= 2 ? "Questions are set." : "Not set yet." %></p>
        <div class="form-grid">
            <div class="form-group">
                <label>Question 1 *</label>
                <select name="question1" class="form-control">
                    <% foreach (var q in HRMS.Services.AccountSecurityService.QuestionCatalog) { %>
                    <option value="<%= q.Key %>"><%= Server.HtmlEncode(q.Text) %></option>
                    <% } %>
                </select>
                <input type="text" name="answer1" class="form-control" style="margin-top:.4rem;" autocomplete="off" />
            </div>
            <div class="form-group">
                <label>Question 2 *</label>
                <select name="question2" class="form-control">
                    <% var i = 0; foreach (var q in HRMS.Services.AccountSecurityService.QuestionCatalog) { i++; %>
                    <option value="<%= q.Key %>" <%= i == 2 ? "selected" : "" %>><%= Server.HtmlEncode(q.Text) %></option>
                    <% } %>
                </select>
                <input type="text" name="answer2" class="form-control" style="margin-top:.4rem;" autocomplete="off" />
            </div>
            <div class="form-group">
                <label>Current password *</label>
                <input type="password" name="questionsPassword" class="form-control" />
            </div>
        </div>
        <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='SaveQuestions';">Save questions</button>
    </div>
</div>
<% } %>
</asp:Content>
