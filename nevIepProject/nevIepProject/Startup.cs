using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(nevIepProject.Startup))]
[assembly: log4net.Config.XmlConfigurator(ConfigFile = "Web.config", Watch = true)]
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
