// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System;
using Ghosts.Client.Handlers;
using Ghosts.Client.Infrastructure;
using Ghosts.Contracts.Interfaces;
using Ghosts.Domain;
using OpenQA.Selenium.Chrome;

namespace Ghosts.Client.InterfaceImpl
{
    public class BrowserChrome : BaseBrowserHandler, IBrowserChrome
    {
        public bool CallHandlerAction(Timeline timeline, TimelineHandler handler)
        {
            try
            {
                EmulateBrowserChromeAction(handler);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        private void EmulateBrowserChromeAction(TimelineHandler handler)
        {
            this.BrowserType = HandlerType.BrowserChrome;
            try
            {
                var options = new ChromeOptions();
                options.AddArguments("disable-infobars");
                options.AddArguments("disable-logging");
                options.AddArguments("--disable-logging");
                options.AddArgument("--log-level=3");
                options.AddArgument("--silent");
                
                options.AddUserProfilePreference("download.default_directory", @"%homedrive%%homepath%\\Downloads");
                options.AddUserProfilePreference("disable-popup-blocking", "true");

                if (handler.HandlerArgs != null)
                {
                    if (handler.HandlerArgs.ContainsKey("isheadless") && handler.HandlerArgs["isheadless"] == "true")
                    {
                        options.AddArguments("headless");
                    }
                    if (handler.HandlerArgs.ContainsKey("blockstyles") && handler.HandlerArgs["blockstyles"] == "true")
                    {
                        options.AddUserProfilePreference("profile.managed_default_content_settings.stylesheets", 2);
                    }
                    if (handler.HandlerArgs.ContainsKey("blockimages") && handler.HandlerArgs["blockimages"] == "true")
                    {
                        options.AddUserProfilePreference("profile.managed_default_content_settings.images", 2);
                    }
                    if (handler.HandlerArgs.ContainsKey("blockflash") && handler.HandlerArgs["blockflash"] == "true")
                    {
                        // ?
                    }
                    if (handler.HandlerArgs.ContainsKey("blockscripts") && handler.HandlerArgs["blockscripts"] == "true")
                    {
                        options.AddUserProfilePreference("profile.managed_default_content_settings.javascript", 1);
                    }
                }
                
                options.AddUserProfilePreference("profile.default_content_setting_values.notifications", 2);
                options.AddUserProfilePreference("profile.managed_default_content_settings.cookies", 2);
                options.AddUserProfilePreference("profile.managed_default_content_settings.plugins", 2);
                options.AddUserProfilePreference("profile.managed_default_content_settings.popups", 2);
                options.AddUserProfilePreference("profile.managed_default_content_settings.geolocation", 2);
                options.AddUserProfilePreference("profile.managed_default_content_settings.media_stream", 2);

                if (!string.IsNullOrEmpty(Program.Configuration.ChromeExtensions))
                {
                    options.AddArguments($"--load-extension={ Program.Configuration.ChromeExtensions }");
                }

                this.Driver = new ChromeDriver(options);
                base.Driver = this.Driver;

                Driver.Navigate().GoToUrl(handler.Initial);

                if (handler.Loop)
                {
                    while (true)
                    {
                        this.ExecuteEvents(handler);
                    }
                }
                else
                {
                    this.ExecuteEvents(handler);
                }
            }
            catch (Exception e)
            {
                _log.Error(e);
            }
            finally
            {
                ProcessManager.KillProcessAndChildrenByName(ProcessManager.ProcessNames.Chrome);
                ProcessManager.KillProcessAndChildrenByName(ProcessManager.ProcessNames.ChromeDriver);
            }
        }

        
    }
}
