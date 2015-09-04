using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(nevIepProject.Startup))]
namespace nevIepProject
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
