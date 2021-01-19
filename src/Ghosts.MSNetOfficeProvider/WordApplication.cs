using NetOffice.WordApi.Enums;
using System;

namespace Ghosts.MSNetOfficeProvider
{
    public class WordApplication
    {
        public WordApplicationData CreateWordApplicationInstance()
        {
            WordApplicationData applicationData = null;
            NetOffice.WordApi.Application wordApplication = new NetOffice.WordApi.Application
            {
                DisplayAlerts = WdAlertLevel.wdAlertsNone,
                Visible = true
            };
            
            // add a new document
            NetOffice.WordApi.Document newDocument = wordApplication.Documents.Add();
            try
            {
                wordApplication.WindowState = WdWindowState.wdWindowStateMinimize;
                foreach (NetOffice.WordApi.Document item in wordApplication.Documents)
                {
                    item.Windows[1].WindowState = WdWindowState.wdWindowStateMinimize;
                }
                
                applicationData = new WordApplicationData()
                { wordApplication = wordApplication, newDocument = newDocument };

            }
            catch (Exception e)
            {
                throw e;
            }
            return applicationData;
        }
       

    }
   
}
