using Ghosts.Domain.Code;

namespace Ghosts.ConfigProvider
{
    public static class ApplicationConfigProvider
    {
        public static ClientConfiguration Configuration 
        { 
            get { return ClientConfigurationLoader.Config; } 
        }
    
    }
}
