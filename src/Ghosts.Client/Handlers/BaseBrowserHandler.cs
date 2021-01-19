﻿// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System;
using System.Threading;
using Ghosts.Client.Infrastructure.Browser;
using Ghosts.Domain;
using Ghosts.Domain.Code;
using NLog;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;

namespace Ghosts.Client.Handlers
{
    public abstract class BaseBrowserHandler : BaseHandler
    {
        public static readonly Logger _log = LogManager.GetCurrentClassLogger();
        public IWebDriver Driver { get; set; }
        public HandlerType BrowserType { get; set; }
        private int _stickiness = 0;
        private int _depthMin = 1;
        private int _depthMax = 10;

        public void ExecuteEvents(TimelineHandler handler)
        {
            try
            {
                foreach (TimelineEvent timelineEvent in handler.TimeLineEvents)
                {
                    WorkingHours.Is(handler);

                    if (timelineEvent.DelayBefore > 0)
                    {
                        Thread.Sleep(timelineEvent.DelayBefore);
                    }

                    RequestConfiguration config;

                    IWebElement element;
                    Actions actions;

                    switch (timelineEvent.Command)
                    {
                        case "random":

                            // setup
                            if (handler.HandlerArgs.ContainsKey("stickiness"))
                            {
                                int.TryParse(handler.HandlerArgs["stickiness"], out _stickiness);
                            }
                            if (handler.HandlerArgs.ContainsKey("stickiness-depth-min"))
                            {
                                int.TryParse(handler.HandlerArgs["stickiness-depth-min"], out _depthMin);
                            }
                            if (handler.HandlerArgs.ContainsKey("stickiness-depth-max"))
                            {
                                int.TryParse(handler.HandlerArgs["stickiness-depth-max"], out _depthMax);
                            }

                            while (true)
                            {
                                if (Driver.CurrentWindowHandle == null)
                                {
                                    throw new Exception("Browser window handle not available");
                                }

                                config = RequestConfiguration.Load(timelineEvent.CommandArgs[new Random().Next(0, timelineEvent.CommandArgs.Count)]);
                                if (config.Uri.IsWellFormedOriginalString())
                                {
                                    MakeRequest(config);
                                    Report(handler.HandlerType.ToString(), timelineEvent.Command, config.ToString(), timelineEvent.TrackableId);

                                    if (this._stickiness > 0)
                                    {
                                        var random = new Random();
                                        //now some percentage of the time should stay on this site
                                        if (random.Next(100) < this._stickiness)
                                        {
                                            var loops = random.Next(this._depthMin, this._depthMax);
                                            for (var loopNumber = 0; loopNumber < loops; loopNumber++)
                                            {
                                                try
                                                {
                                                    var linkManager = new LinkManager(config.GetHost());


                                                    //get all links
                                                    var links = Driver.FindElements(By.TagName("a"));
                                                    foreach (var l in links)
                                                    {
                                                        var node = l.GetAttribute("href");
                                                        if (string.IsNullOrEmpty(node) ||
                                                            node.ToLower().StartsWith("//"))
                                                        {
                                                            //skip, these seem ugly
                                                        }
                                                        // http|s links
                                                        else if (node.ToLower().StartsWith("http"))
                                                        {
                                                            linkManager.AddLink(node.ToLower());
                                                        }
                                                        // relative links - prefix the scheme and host 
                                                        else
                                                        {
                                                            linkManager.AddLink($"{config.GetHost()}{node.ToLower()}");
                                                        }
                                                    }

                                                    var link = linkManager.Choose();
                                                    if (link == null)
                                                    {
                                                        return;
                                                    }

                                                    config.Method = "GET";
                                                    config.Uri = link.Url;

                                                    MakeRequest(config);
                                                    Report(handler.HandlerType.ToString(), timelineEvent.Command, config.ToString(), timelineEvent.TrackableId);
                                                }
                                                catch (Exception e)
                                                {
                                                    _log.Error(e);
                                                }

                                                Thread.Sleep(timelineEvent.DelayAfter);
                                            }
                                        }
                                    }
                                }
                                Thread.Sleep(timelineEvent.DelayAfter);
                            }
                        case "browse":
                            config = RequestConfiguration.Load(timelineEvent.CommandArgs[0]);
                            if (config.Uri.IsWellFormedOriginalString())
                            {
                                MakeRequest(config);
                                Report(handler.HandlerType.ToString(), timelineEvent.Command, config.ToString(), timelineEvent.TrackableId);
                            }
                            break;
                        case "download":
                            if (timelineEvent.CommandArgs.Count > 0)
                            {
                                element = Driver.FindElement(By.XPath(timelineEvent.CommandArgs[0].ToString()));
                                element.Click();
                                Report(handler.HandlerType.ToString(), timelineEvent.Command, string.Join(",", timelineEvent.CommandArgs), timelineEvent.TrackableId);
                                Thread.Sleep(1000);
                            }
                            break;
                        case "type":
                            element = Driver.FindElement(By.Name(timelineEvent.CommandArgs[0].ToString()));
                            actions = new Actions(Driver);
                            actions.SendKeys(element, timelineEvent.CommandArgs[1].ToString()).Build().Perform();
                            break;
                        case "typebyid":
                            element = Driver.FindElement(By.Id(timelineEvent.CommandArgs[0].ToString()));
                            actions = new Actions(Driver);
                            actions.SendKeys(element, timelineEvent.CommandArgs[1].ToString()).Build().Perform();
                            break;
                        case "click":
                            element = Driver.FindElement(By.Name(timelineEvent.CommandArgs[0].ToString()));
                            actions = new Actions(Driver);
                            actions.MoveToElement(element).Click().Perform();
                            break;
                        case "clickbyid":
                            element = Driver.FindElement(By.Id(timelineEvent.CommandArgs[0].ToString()));
                            actions = new Actions(Driver);
                            actions.MoveToElement(element).Click().Perform();
                            break;
                    }

                    if (timelineEvent.DelayAfter > 0)
                    {
                        Thread.Sleep(timelineEvent.DelayAfter);
                    }
                }
            }
            catch (Exception e)
            {
                _log.Error(e);
            }
        }

        private void MakeRequest(RequestConfiguration config)
        {
            switch (config.Method.ToUpper())
            {
                case "GET":
                    Driver.Navigate().GoToUrl(config.Uri);
                    break;
                case "POST":
                case "PUT":
                case "DELETE":
                    Driver.Navigate().GoToUrl("about:blank");
                    var script = "var xhr = new XMLHttpRequest();";
                    script += $"xhr.open('{config.Method.ToUpper()}', '{config.Uri}', true);";
                    script += "xhr.setRequestHeader('Content-type', 'application/x-www-form-urlencoded');";
                    script += "xhr.onload = function() {";
                    script += "document.write(this.responseText);";
                    script += "};";
                    script += $"xhr.send('{config.FormValues.ToFormValueString()}');";

                    var javaScriptExecutor = (IJavaScriptExecutor)Driver;
                    javaScriptExecutor.ExecuteScript(script);
                    break;
            }
        }

        /// <summary>
        /// Close browser
        /// </summary>
        public void Close()
        {
            Report(BrowserType.ToString(), "Close", string.Empty);
            Driver.Close();
        }

        public void Stop()
        {
            Report(BrowserType.ToString(), "Stop", string.Empty);
            Close();
        }
    }
}