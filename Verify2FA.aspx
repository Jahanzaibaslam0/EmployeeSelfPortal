<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Verify2FA.aspx.cs" Inherits="HRMS.Verify2FAPage" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="UTF-8" />
    <title>Ghazi Workstation &ndash; Authenticator</title>
    <link rel="stylesheet" href="/css/style.css?v=11" />
    <style>
        body { min-height:100vh; display:flex; align-items:center; justify-content:center;
               background:linear-gradient(135deg, var(--gb-blue) 0%, var(--gb-blue-dark) 55%, #1a1d5c 100%); }
        .login-card { width:100%; max-width:420px; background:#fff; border-radius:10px;
                      box-shadow:0 20px 60px rgba(46,49,146,.35); overflow:hidden; }
        .login-header { padding:1.5rem 2rem 1rem; text-align:center; border-bottom:4px solid var(--gb-red); }
        .login-body { padding:2rem; }
    </style>
</head>
<body>
<form id="form1" runat="server">
<div class="login-card">
    <div class="login-header">
        <h1 style="margin:0;color:var(--gb-blue);">Two-factor sign-in</h1>
        <p style="color:var(--gb-red);font-weight:600;text-transform:uppercase;font-size:.82rem;">Internal authenticator code</p>
    </div>
    <div class="login-body">
        <% if (!string.IsNullOrEmpty(ErrorMessage)) { %>
        <div class="alert alert-error" style="margin-bottom:1rem;"><%= Server.HtmlEncode(ErrorMessage) %></div>
        <% } %>
        <p class="text-muted" style="margin-bottom:1rem;">Enter the 6-digit code from your authenticator app, or a one-time backup code.</p>
        <div class="form-group">
            <label>Code</label>
            <input type="text" name="code" class="form-control" maxlength="12" autocomplete="one-time-code" autofocus />
        </div>
        <button type="submit" class="btn btn-primary" style="width:100%;margin-top:.5rem;">Verify</button>
        <p style="margin-top:1rem;text-align:center;"><a href="/Login.aspx">Back to sign-in</a></p>
    </div>
</div>
</form>
</body>
</html>
