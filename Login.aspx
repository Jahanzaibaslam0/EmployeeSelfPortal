<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="HRMS.LoginPage" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="UTF-8" />
    <title>HRMS – Login</title>
    <link rel="stylesheet" href="/css/style.css?v=9" />
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
        <img src="/images/gb-logo.png" alt="Ghazi Brothers" style="height:52px;margin-bottom:.75rem;" onerror="this.style.display='none'" />
        <h1 style="margin:0;color:var(--gb-blue);">HRMS</h1>
        <p style="color:var(--gb-red);font-weight:600;text-transform:uppercase;font-size:.82rem;">Human Resource Management System</p>
    </div>
    <div class="login-body">
        <% if (!string.IsNullOrEmpty(ErrorMessage)) { %>
        <div class="alert alert-error" style="margin-bottom:1rem;"><%= ErrorMessage %></div>
        <% } %>
        <div class="form-group">
            <label>Username <span class="required">*</span></label>
            <asp:TextBox ID="txtUsername" runat="server" CssClass="form-control" />
        </div>
        <div class="form-group">
            <label>Password <span class="required">*</span></label>
            <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" CssClass="form-control" />
        </div>
        <asp:Button ID="btnLogin" runat="server" Text="Sign In" CssClass="btn btn-primary"
                    style="width:100%;margin-top:.5rem;" OnClick="btnLogin_Click" />
    </div>
</div>
</form>
</body>
</html>
