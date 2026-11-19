<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ForgotPassword.aspx.cs" Inherits="HRMS.ForgotPasswordPage" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="UTF-8" />
    <title>Ghazi Workstation &ndash; Recover password</title>
    <link rel="stylesheet" href="/css/style.css?v=11" />
    <style>
        body { min-height:100vh; display:flex; align-items:center; justify-content:center;
               background:linear-gradient(135deg, var(--gb-blue) 0%, var(--gb-blue-dark) 55%, #1a1d5c 100%); }
        .login-card { width:100%; max-width:460px; background:#fff; border-radius:10px;
                      box-shadow:0 20px 60px rgba(46,49,146,.35); overflow:hidden; }
        .login-header { padding:1.5rem 2rem 1rem; text-align:center; border-bottom:4px solid var(--gb-red); }
        .login-body { padding:2rem; }
    </style>
</head>
<body>
<form id="form1" runat="server">
<input type="hidden" name="__handler" id="__handler" value="" />
<div class="login-card">
    <div class="login-header">
        <h1 style="margin:0;color:var(--gb-blue);">Recover password</h1>
        <p style="color:var(--gb-red);font-weight:600;text-transform:uppercase;font-size:.82rem;">Internal server recovery</p>
    </div>
    <div class="login-body">
        <% if (!string.IsNullOrEmpty(ErrorMessage)) { %>
        <div class="alert alert-error" style="margin-bottom:1rem;"><%= Server.HtmlEncode(ErrorMessage) %></div>
        <% } %>
        <% if (!string.IsNullOrEmpty(InfoMessage)) { %>
        <div class="alert alert-info" style="margin-bottom:1rem;"><%= Server.HtmlEncode(InfoMessage) %></div>
        <% } %>

        <% if (Step == "username") { %>
        <p class="text-muted" style="margin-bottom:1rem;">Recovery runs on this server only. No email is sent.</p>
        <div class="form-group">
            <label>Username</label>
            <input type="text" name="username" class="form-control" autocomplete="username" />
        </div>
        <button type="submit" class="btn btn-primary" style="width:100%;margin-top:.5rem;" onclick="document.getElementById('__handler').value='Start';">Continue</button>
        <% } else if (Step == "verify") { %>
        <% foreach (var q in Questions) { %>
        <div class="form-group">
            <label><%= Server.HtmlEncode(q.QuestionText) %></label>
            <input type="text" name="ans_<%= Server.HtmlEncode(q.QuestionKey) %>" class="form-control" autocomplete="off" />
        </div>
        <% } %>
        <div class="form-group">
            <label>Or backup code (optional)</label>
            <input type="text" name="backupCode" class="form-control" autocomplete="off" />
        </div>
        <button type="submit" class="btn btn-primary" style="width:100%;margin-top:.5rem;" onclick="document.getElementById('__handler').value='Verify';">Verify</button>
        <% } else if (Step == "reset") { %>
        <p class="text-muted"><%= Server.HtmlEncode(HRMS.Services.PasswordHelper.PolicySummary) %></p>
        <div class="form-group">
            <label>New password</label>
            <input type="password" name="newPassword" class="form-control" autocomplete="new-password" />
        </div>
        <div class="form-group">
            <label>Confirm password</label>
            <input type="password" name="confirmPassword" class="form-control" autocomplete="new-password" />
        </div>
        <button type="submit" class="btn btn-primary" style="width:100%;margin-top:.5rem;" onclick="document.getElementById('__handler').value='Reset';">Set new password</button>
        <% } else { %>
        <a href="/Login.aspx" class="btn btn-primary" style="width:100%;display:block;text-align:center;">Sign in</a>
        <% } %>

        <p style="margin-top:1rem;text-align:center;"><a href="/Login.aspx">Back to sign-in</a></p>
        <p class="text-muted" style="margin-top:.75rem;font-size:.8rem;text-align:center;">Administrators can also reset a password and turn off 2FA in User Setup.</p>
    </div>
</div>
</form>
</body>
</html>
