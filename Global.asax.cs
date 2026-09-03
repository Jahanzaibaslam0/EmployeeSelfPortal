using System;
using System.Web;

namespace HRMS
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            StartupMigrations.Run();
            StartupMigrations.Seed();
        }
    }
}
