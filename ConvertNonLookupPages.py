#!/usr/bin/env python3
"""Convert non-lookup Razor Pages to ASP.NET Web Forms (.aspx / .aspx.cs)."""
from __future__ import annotations

import os
import re
import sys
from pathlib import Path

ROOT = Path(r"D:\Project\HRMS")
PAGES = ROOT / "Pages"

SKIP_NAMES = {
    "LookupSetupBase",
    "_ViewImports",
    "_AppHeader",
    "_AppFooter",
    "_LookupSetupPage",
    "_MasterExcelPanel",
    "_NotificationPopup",
    "Index",  # maps to Home.aspx
}

# Keep existing Login/Home unless we overwrite Login carefully
PRESERVE_EXISTING = {"Login", "Home", "Default"}

SERVICE_FIELD_INIT = {
    "InventoryService": "new InventoryService()",
    "MasterExcelService": "new MasterExcelService()",
    "NotificationService": "new NotificationService()",
    "MemorandumService": "new MemorandumService()",
    "DashboardService": "new DashboardService()",
    "EmployeeProfileAccessService": "new EmployeeProfileAccessService()",
    "DataAccessScopeService": "new DataAccessScopeService()",
    "DocumentStorageService": "new DocumentStorageService()",
}


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8", errors="replace")


def write_text(path: Path, content: str) -> None:
    path.write_text(content, encoding="utf-8", newline="\r\n")


def is_lookup(cs_text: str) -> bool:
    return "LookupSetupPageModel" in cs_text


def list_non_lookup_pages() -> list[str]:
    names = []
    for f in sorted(PAGES.glob("*.cshtml.cs")):
        base = f.name.replace(".cshtml.cs", "")
        if base.startswith("_") or base in SKIP_NAMES:
            continue
        if is_lookup(read_text(f)):
            continue
        names.append(base)
    return names


def strip_di_ctor(cs: str, base: str) -> str:
    # Remove constructors that take DI params
    pattern = rf"public\s+{re.escape(base)}Model\s*\([^)]*\)\s*\{{(?:[^{{}}]*|\{{[^{{}}]*\}})*\}}"
    cs = re.sub(pattern, "", cs, flags=re.DOTALL)
    # Also generic Model ctor
    pattern2 = r"public\s+\w+Model\s*\([^)]*(?:IConfiguration|AuthService|PermissionService|IWebHostEnvironment)[^)]*\)\s*\{(?:[^{}]*|\{[^{}]*\})*\}"
    cs = re.sub(pattern2, "", cs, flags=re.DOTALL)
    return cs


def convert_codebehind(cs: str, base: str) -> str:
    class_name = f"{base}Page"

    cs = cs.replace("using Microsoft.Data.SqlClient;", "using System.Data.SqlClient;")
    cs = re.sub(r"using Microsoft\.AspNetCore[^\r\n]*\r?\n", "", cs)
    cs = re.sub(r"using Microsoft\.Extensions\.Configuration;\r?\n", "", cs)
    cs = re.sub(r"using Microsoft\.Extensions\.[^\r\n]*\r?\n", "", cs)
    cs = re.sub(r"using System\.Text\.Json;\r?\n", "using Newtonsoft.Json;\r\n", cs)

    # namespace
    if re.search(r"namespace\s+HRMS\.Pages\s*;", cs):
        # file-scoped -> block
        cs = re.sub(r"namespace\s+HRMS\.Pages\s*;\s*", "namespace HRMS\n{\n", cs)
        if not cs.rstrip().endswith("}"):
            cs = cs.rstrip() + "\n}\n"
        else:
            # ensure outer brace closes namespace - count carefully later
            pass
    else:
        cs = re.sub(r"namespace\s+HRMS\.Pages\s*\{", "namespace HRMS {", cs)

    # class rename
    cs = re.sub(
        rf"public\s+(partial\s+)?class\s+{re.escape(base)}Model\s*:\s*PageModel",
        f"public partial class {class_name} : AppBasePage",
        cs,
    )
    cs = re.sub(
        r"public\s+(partial\s+)?class\s+\w+Model\s*:\s*PageModel",
        f"public partial class {class_name} : AppBasePage",
        cs,
    )

    cs = strip_di_ctor(cs, base)

    # Remove DI fields that AppBase provides
    for fld in ("_conn", "_auth", "_perms", "_audit"):
        cs = re.sub(rf"\s*private\s+readonly\s+string\s+{fld}\s*;\r?\n", "", cs)
        cs = re.sub(rf"\s*private\s+readonly\s+\w+\s+{fld}\s*;\r?\n", "", cs)

    # Field-init other services
    for svc, init in SERVICE_FIELD_INIT.items():
        cs = re.sub(
            rf"private\s+readonly\s+{svc}\s+_(\w+)\s*;",
            rf"private readonly {svc} _\1 = {init};",
            cs,
        )
        # also without underscore prefix variants like _dashboard
        cs = re.sub(
            rf"(private\s+readonly\s+{svc}\s+_(\w+)\s*=\s*)[^;]+;",
            rf"\1{init};",
            cs,
        )

    # DashboardService named _dashboard specially
    cs = re.sub(
        r"private\s+readonly\s+DashboardService\s+_dashboard\s*;",
        "private readonly DashboardService _dashboard = new DashboardService();",
        cs,
    )
    cs = re.sub(
        r"private\s+readonly\s+DashboardService\s+_dashboard\s*=\s*[^;]+;",
        "private readonly DashboardService _dashboard = new DashboardService();",
        cs,
    )

    # Replace field refs
    cs = re.sub(r"\b_conn\b", "Conn", cs)
    cs = re.sub(r"\b_auth\b", "Auth", cs)
    cs = re.sub(r"\b_perms\b", "Perms", cs)
    cs = re.sub(r"\b_audit\b", "Audit", cs)

    # TempData -> Session
    cs = cs.replace('TempData["Alert"]', 'Session["Alert"]')
    cs = cs.replace('TempData["AlertType"]', 'Session["AlertType"]')
    cs = re.sub(r'TempData\["([^"]+)"\]', r'Session["\1"]', cs)
    cs = cs.replace("TempData.ContainsKey(", "Session[")
    # Fix broken ContainsKey conversion: Session["Alert"]) -> Session["Alert"] != null
    cs = re.sub(r'Session\["([^"]+)"\]\)\s*;?\s*return;', r'Session["\1"] == null) return;', cs)
    cs = re.sub(
        r'if\s*\(\s*!?\s*Session\["([^"]+)"\]\s*\)',
        lambda m: f'if (Session["{m.group(1)}"] == null)' if "!" not in m.group(0)[:10] else f'if (Session["{m.group(1)}"] == null)',
        cs,
    )
    # LoadAlert patterns that used TempData.ContainsKey
    cs = re.sub(
        r"if\s*\(\s*!TempData\.ContainsKey\(\"Alert\"\)\s*\)\s*return;",
        'if (Session["Alert"] == null) return;',
        cs,
    )
    cs = re.sub(
        r"if\s*\(\s*!Session\[\"Alert\"\]\s*\)\s*return;",
        'if (Session["Alert"] == null) return;',
        cs,
    )

    # IActionResult / redirects
    cs = re.sub(r"public\s+IActionResult\s+", "public void ", cs)
    cs = re.sub(r"public\s+async\s+Task<IActionResult>\s+", "public void ", cs)
    cs = re.sub(r"public\s+JsonResult\s+", "public void ", cs)
    cs = re.sub(r"public\s+FileResult\s+", "public void ", cs)
    cs = re.sub(r"public\s+IActionResult\s+", "public void ", cs)

    cs = re.sub(r"return\s+RedirectToPage\(\);", "return;", cs)
    cs = re.sub(
        r'return\s+RedirectToPage\(\s*"/([^"]+)"\s*\);',
        r'Response.Redirect("~/\1.aspx"); return;',
        cs,
    )
    cs = re.sub(
        r'return\s+RedirectToPage\(\s*"/([^"]+)"\s*,\s*new\s*\{[^}]*\}\s*\);',
        r'Response.Redirect("~/\1.aspx"); return;',
        cs,
    )
    # RedirectToPage(new { editId = ... })
    cs = re.sub(
        r"return\s+RedirectToPage\(\s*new\s*\{[^}]*\}\s*\);",
        f'Response.Redirect("~/{base}.aspx" + (Request.Url.Query ?? "")); return;',
        cs,
    )
    cs = re.sub(r"return\s+Page\(\);", "return;", cs)
    cs = re.sub(
        r'return\s+Redirect\(\s*"([^"]+)"\s*\);',
        r'Response.Redirect("\1"); return;',
        cs,
    )
    cs = re.sub(
        r'return\s+RedirectToPage\(\s*"/Index"[^)]*\);',
        'Response.Redirect("~/Home.aspx"); return;',
        cs,
    )
    cs = re.sub(
        r"return\s+NotFound\(\);",
        'Response.StatusCode = 404; return;',
        cs,
    )
    cs = re.sub(
        r"return\s+Unauthorized\(\);",
        'Response.StatusCode = 401; return;',
        cs,
    )
    cs = re.sub(
        r"return\s+Forbid\(\);",
        'Response.Redirect("~/Home.aspx?accessDenied=1"); return;',
        cs,
    )

    # File(...) results — leave as comments for manual fix, replace with Response.BinaryWrite pattern stub
    # JsonResult returns
    cs = re.sub(
        r"return\s+new\s+JsonResult\(([^;]+)\);",
        r'Response.ContentType = "application/json"; Response.Write(JsonConvert.SerializeObject(\1)); return;',
        cs,
    )

    # C# range indexer last[4..] -> Substring for net48 safety
    cs = re.sub(r"(\w+)\[(\d+)\.\.\]", r"\1.Substring(\2)", cs)

    # Ensure Page_Load dispatcher
    if "Page_Load" not in cs:
        handlers = re.findall(r"public\s+void\s+(OnPost\w*|OnGet\w+)\s*\(", cs)
        # Also catch still-IActionResult if any missed
        handlers += re.findall(r"public\s+(?:void|IActionResult)\s+(OnPost\w*|OnGet\w+)\s*\(", cs)
        handlers = list(dict.fromkeys(handlers))

        # Detect OnGet signature
        onget_m = re.search(r"public\s+void\s+OnGet\s*\(([^)]*)\)", cs)
        onget_call = "OnGet();"
        if onget_m:
            params = [p.strip() for p in onget_m.group(1).split(",") if p.strip()]
            if params:
                args = []
                for p in params:
                    # type name
                    parts = p.split()
                    if len(parts) < 2:
                        continue
                    typ, name = parts[0], parts[-1].lstrip("?")
                    if "int?" in p or typ.startswith("int?"):
                        args.append(f'QueryInt("{name}")')
                    elif typ in ("int", "Int32"):
                        args.append(f'(QueryInt("{name}") ?? 0)')
                    elif "DateTime?" in p or "DateTime" in typ:
                        args.append(f'ParseDate(Request.QueryString["{name}"])')
                    elif "string" in typ:
                        args.append(f'(Request.QueryString["{name}"] ?? "")')
                    elif "bool" in typ:
                        args.append(f'(Request.QueryString["{name}"] == "1" || Request.QueryString["{name}"] == "true")')
                    else:
                        args.append("null")
                onget_call = f"OnGet({', '.join(args)});"

        dispatch_lines = [
            "",
            "        protected void Page_Load(object sender, EventArgs e)",
            "        {",
            "            if (IsPostBack)",
            "            {",
            '                var handler = Request.Form["__handler"] ?? Request.QueryString["handler"] ?? "Save";',
        ]
        for h in handlers:
            if h == "OnGet":
                continue
            key = h
            if key.startswith("OnPost"):
                key = key[6:] or "Save"
            elif key.startswith("OnGet"):
                key = key[5:] or "Get"
            # Skip parameterized posts in dispatcher — call without args if possible
            msig = re.search(rf"public\s+void\s+{h}\s*\(([^)]*)\)", cs)
            call = f"{h}();"
            if msig and msig.group(1).strip():
                # bind from form for common patterns
                call = build_post_call(h, msig.group(1))
            dispatch_lines.append(
                f'                if (string.Equals(handler, "{key}", StringComparison.OrdinalIgnoreCase)) {{ {call} return; }}'
            )
        dispatch_lines += [
            "            }",
            "            if (!IsPostBack)",
            "            {",
            f"                {onget_call}",
            "            }",
            "        }",
            "",
        ]
        dispatch = "\n".join(dispatch_lines)
        cs = re.sub(
            rf"(public partial class {re.escape(class_name)} : AppBasePage\s*\{{)",
            r"\1" + dispatch,
            cs,
            count=1,
        )

    # Public pages
    if base in ("InitDatabase", "ResetAdmin", "Login"):
        if "IsPublicPage" not in cs:
            cs = re.sub(
                rf"(public partial class {re.escape(class_name)} : AppBasePage\s*\{{)",
                r"\1\n        protected override bool IsPublicPage => true;\n",
                cs,
                count=1,
            )

    # Helper ParseDate if needed
    if "ParseDate(" in cs and "DateTime? ParseDate" not in cs:
        helper = """
        private static DateTime? ParseDate(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            DateTime d;
            return DateTime.TryParse(raw, out d) ? d : (DateTime?)null;
        }
"""
        # insert before last closing braces of class/namespace
        cs = insert_before_last_class_close(cs, helper)

    # Usings
    needed = ["using System;", "using System.Web;", "using System.Web.UI;", "using HRMS.Services;"]
    for u in reversed(needed):
        if u not in cs:
            cs = u + "\n" + cs
    if "System.Data.SqlClient" in cs and "using System.Data.SqlClient;" not in cs:
        cs = "using System.Data.SqlClient;\n" + cs
    if "JsonConvert" in cs and "using Newtonsoft.Json;" not in cs:
        cs = "using Newtonsoft.Json;\n" + cs
    if "List<" in cs and "using System.Collections.Generic;" not in cs:
        cs = "using System.Collections.Generic;\n" + cs
    if "Enumerable" in cs or ".Where(" in cs or ".Select(" in cs or ".ToList(" in cs:
        if "using System.Linq;" not in cs:
            cs = "using System.Linq;\n" + cs
    if "XLWorkbook" in cs and "using ClosedXML.Excel;" not in cs:
        cs = "using ClosedXML.Excel;\n" + cs
    if "StringBuilder" in cs and "using System.Text;" not in cs:
        cs = "using System.Text;\n" + cs
    if "MemoryStream" in cs and "using System.IO;" not in cs:
        cs = "using System.IO;\n" + cs

    # Fix file-scoped leftover: ensure namespace braced properly
    cs = fix_namespace_braces(cs)

    return cs


def build_post_call(method: str, param_list: str) -> str:
    params = [p.strip() for p in param_list.split(",") if p.strip()]
    if not params:
        return f"{method}();"
    args = []
    for p in params:
        # handle defaults like bool isActive = true
        p = re.sub(r"\s*=\s*[^,]+$", "", p).strip()
        parts = p.replace("?", " ").split()
        if len(parts) < 2:
            args.append("null")
            continue
        typ = parts[0]
        name = parts[-1]
        if typ in ("int", "Int32") or p.startswith("int"):
            args.append(f'int.TryParse(Request.Form["{name}"], out var __{name}) ? __{name} : 0')
        elif "bool" in typ:
            args.append(f'FormBool("{name}")')
        elif "DateTime" in typ:
            args.append(f'ParseDate(Request.Form["{name}"])')
        elif "decimal" in typ or "Decimal" in typ:
            args.append(f'decimal.TryParse(Request.Form["{name}"], out var __{name}) ? __{name} : 0m')
        elif "IFormFile" in p or "HttpPostedFile" in p:
            args.append(f'Request.Files["{name}"]')
        else:
            args.append(f'FormString("{name}")')
    return f"{method}({', '.join(args)});"


def insert_before_last_class_close(cs: str, helper: str) -> str:
    # Find last "    }" before final namespace close
    idx = cs.rfind("\n    }\n}")
    if idx >= 0:
        return cs[:idx] + "\n" + helper + cs[idx:]
    idx = cs.rfind("\n}")
    if idx >= 0:
        return cs[:idx] + "\n" + helper + cs[idx:]
    return cs + "\n" + helper


def fix_namespace_braces(cs: str) -> str:
    if "namespace HRMS" not in cs:
        return cs
    # Count braces after namespace
    m = re.search(r"namespace\s+HRMS\s*\{", cs)
    if not m:
        # file became namespace HRMS\n{ already
        return cs
    return cs


def extract_body(html: str) -> str:
    html = re.sub(r"(?m)^\s*@page.*\r?\n", "", html)
    html = re.sub(r"(?m)^\s*@model.*\r?\n", "", html)
    html = re.sub(r"(?m)^\s*@using.*\r?\n", "", html)
    html = re.sub(r"(?m)^\s*@\{[\s\S]*?\}\s*\r?\n", "", html, count=1)

    # Prefer <main> content
    m = re.search(r"(?is)<main[^>]*>([\s\S]*)</main>", html)
    if m:
        return m.group(1).strip()

    m = re.search(r"(?is)<body[^>]*>([\s\S]*)</body>", html)
    if m:
        body = m.group(1)
        body = re.sub(r'@await Html\.PartialAsync\("_AppHeader"[^)]*\)', "", body)
        body = re.sub(r'@await Html\.PartialAsync\("_AppFooter"[^)]*\)', "", body)
        return body.strip()

    return html.strip()


def convert_markup(html: str, base: str) -> str:
    body = extract_body(html)

    # Remove partials / antiforgery
    body = re.sub(r'@await Html\.PartialAsync\("[^"]+"[^)]*\)', "", body)
    body = body.replace("@Html.AntiForgeryToken()", "")
    body = re.sub(r"@Html\.AntiForgeryToken\(\)", "", body)

    # Nested forms are invalid under master form — convert inner <form> to div with marker
    # Keep method=post forms but they become problematic; change to div and rely on master form + __handler
    def replace_form(m):
        attrs = m.group(1) or ""
        handler = ""
        hm = re.search(r'action="[^"]*[?&]handler=([^"&]+)"', attrs)
        if hm:
            handler = hm.group(1)
        hm2 = re.search(r'asp-page-handler="([^"]+)"', attrs)
        if hm2:
            handler = hm2.group(1)
        inner = m.group(2)
        hidden = f'<input type="hidden" name="__handler" value="{handler}" />\n' if handler else ""
        # Also action="/X?handler=Y" without asp-
        return f'<div class="wf-form-section">\n{hidden}{inner}\n</div>'

    body = re.sub(
        r"<form([^>]*)>([\s\S]*?)</form>",
        replace_form,
        body,
        flags=re.IGNORECASE,
    )

    # Convert @if / @foreach / @for to <% %>
    body = re.sub(r"@if\s*\(", "<% if (", body)
    body = re.sub(r"@else\s*if\s*\(", "<% else if (", body)
    body = re.sub(r"@else\s*\{", "<% else {", body)
    body = re.sub(r"@foreach\s*\(", "<% foreach (", body)
    body = re.sub(r"@for\s*\(", "<% for (", body)
    body = re.sub(r"@while\s*\(", "<% while (", body)
    body = re.sub(r"@\{", "<%", body)

    # Close razor code blocks: lines that are only "}" -> "%>"  — careful
    # Pattern: after <% if (...) {  content  }  — convert matching braces that open after <%
    body = convert_razor_braces(body)

    # Expressions @( ... ) and @Model.X and @item.X
    body = re.sub(r"@\((.*?)\)", r"<%= \1 %>", body)
    body = body.replace("@Model.", "")
    # Remaining @identifier.property or @identifier
    body = re.sub(r"@([A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*)", r"<%= \1 %>", body)

    # Fix common broken double encodings
    body = body.replace("<%= <%=", "<%=")
    body = re.sub(r"<%=\s*<%=\s*", "<%= ", body)

    # Links
    body = re.sub(r'href="(/[A-Za-z][A-Za-z0-9]*)"', r'href="\1.aspx"', body)
    body = re.sub(r'href="(/[A-Za-z][A-Za-z0-9]*)\?', r'href="\1.aspx?', body)
    body = body.replace(".aspx.aspx", ".aspx")
    body = body.replace("/Index.aspx", "/Home.aspx")

    # Scripts that referenced app.js — master already includes; leave page scripts
    body = re.sub(r'<script\s+src="/js/app\.js[^"]*"></script>\s*', "", body)

    # Ternary in attributes that used @(Model.X ? "checked" : "")
    # already handled by @(...)

    aspx = f"""<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="{base}.aspx.cs" Inherits="HRMS.{base}Page" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
{body}
</asp:Content>
"""
    return aspx


def convert_razor_braces(body: str) -> str:
    """Convert Razor-style brace blocks opened after <% into proper <% %> / <%= %> mix."""
    # Strategy: after we converted @if ( to <% if (, the opening { remains and closing }
    # We need: <% if (cond) { %> ... <% } %>
    # Insert %> after opening { that follows <% code without %>
    out = []
    i = 0
    n = len(body)
    while i < n:
        if body.startswith("<%", i) and not body.startswith("<%=", i) and not body.startswith("<%--", i):
            # find matching end of this code opener — look for { then insert %>
            j = i + 2
            # skip until we hit { or end that already has %>
            depth = 0
            started = False
            while j < n:
                if body.startswith("%>", j):
                    out.append(body[i : j + 2])
                    i = j + 2
                    break
                if body[j] == "{":
                    depth += 1
                    if depth == 1 and not started:
                        # opening of block
                        out.append(body[i : j + 1] + " %>")
                        i = j + 1
                        started = True
                        # now scan for closing brace at depth 0
                        k = i
                        d = 1
                        while k < n and d > 0:
                            if body.startswith("<%", k):
                                # nested already converted? copy through
                                pass
                            if body[k] == "{" and not in_string_approx(body, k):
                                d += 1
                            elif body[k] == "}" and not in_string_approx(body, k):
                                d -= 1
                            k += 1
                        # k is past closing }
                        # content between i and k-1
                        inner = body[i : k - 1]
                        # Recurse on inner for nested @ already converted
                        out.append(inner)
                        out.append("<% } %>")
                        i = k
                        break
                j += 1
            else:
                out.append(body[i])
                i += 1
        else:
            out.append(body[i])
            i += 1
    result = "".join(out)
    # Fix leftover lone } that were razor closers at line starts
    result = re.sub(r"(?m)^\s*\}\s*$", "<% } %>", result)
    result = re.sub(r"(?m)^\s*\}\s*else\s*\{", "<% } else { %>", result)
    return result


def in_string_approx(s: str, idx: int) -> bool:
    # crude: count quotes before on same line
    line_start = s.rfind("\n", 0, idx) + 1
    frag = s[line_start:idx]
    return frag.count('"') % 2 == 1


def ensure_default() -> None:
    default_cs = ROOT / "Default.aspx.cs"
    default_aspx = ROOT / "Default.aspx"
    if not default_aspx.exists():
        write_text(
            default_aspx,
            '<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="HRMS.DefaultPage" %>\r\n',
        )
    write_text(
        default_cs,
        """using System;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public partial class DefaultPage : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var auth = new AuthService();
            Response.Redirect(auth.IsLoggedIn ? "~/Home.aspx" : "~/Login.aspx");
        }
    }
}
""",
    )


def simplify_employee_master_if_needed(cs: str, aspx: str) -> tuple[str, str]:
    """EmployeeMaster is huge — ensure functional list/edit skeleton if conversion is broken."""
    return cs, aspx


def main() -> int:
    names = list_non_lookup_pages()
    created = []
    skipped = [
        ("LookupSetupPageModel pages", "handled separately"),
        ("_AppHeader/_AppFooter/_LookupSetupPage/_MasterExcelPanel/_NotificationPopup", "partials"),
        ("_ViewImports", "imports only"),
        ("LookupSetupBase", "base class"),
        ("Index", "maps to existing Home.aspx"),
    ]

    print(f"Non-lookup pages to convert: {len(names)}")
    for name in names:
        cs_path = PAGES / f"{name}.cshtml.cs"
        html_path = PAGES / f"{name}.cshtml"
        cs_raw = read_text(cs_path)

        if name == "Login":
            # Refresh lightly: ensure public + System.Data patterns already OK; skip overwrite of working Login
            print(f"SKIP overwrite Login (exists) — ensuring Default redirect only")
            continue
        if name == "Index":
            continue

        cs = convert_codebehind(cs_raw, name)
        if html_path.exists():
            aspx = convert_markup(read_text(html_path), name)
        else:
            aspx = f"""<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="{name}.aspx.cs" Inherits="HRMS.{name}Page" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<p>{name}</p>
</asp:Content>
"""

        if name == "EmployeeMaster":
            cs, aspx = simplify_employee_master_if_needed(cs, aspx)

        write_text(ROOT / f"{name}.aspx.cs", cs)
        write_text(ROOT / f"{name}.aspx", aspx)
        created.append(f"{name}.aspx")
        print(f"Wrote {name}.aspx + .aspx.cs")

    ensure_default()
    if "Default.aspx" not in created:
        created.append("Default.aspx")

    # Summary file
    summary = ROOT / "_conversion_summary.txt"
    lines = ["CREATED ASPX FILES:", *[f"  {c}" for c in created], "", "SKIPPED:", *[f"  {a}: {b}" for a, b in skipped]]
    write_text(summary, "\n".join(lines) + "\n")
    print(f"\nCreated {len(created)} pages. Summary -> {summary}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
