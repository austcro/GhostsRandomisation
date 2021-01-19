using System;
using NetOffice.WordApi;
using NetOffice.WordApi.Enums;

namespace Ghosts.NetOfficeProvider
{
    public class WordApplication: IMSOfficeApplication
    {
        public WordApplicationData CreateWordApplicationInstance()
        {
            WordApplicationData applicationData = null;
            Application wordApplication = new Application
            {
                DisplayAlerts = WdAlertLevel.wdAlertsNone,
                Visible = true
            };

            // add a new document
            Document newDocument = wordApplication.Documents.Add();
            try
            {
                wordApplication.WindowState = WdWindowState.wdWindowStateMinimize;
                foreach (Document item in wordApplication.Documents)
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
