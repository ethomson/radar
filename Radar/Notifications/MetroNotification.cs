using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.IO;
using System.Drawing;
using System.Threading;

using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;

using Radar;
using Radar.Clients;
using Radar.Util;

namespace Radar.Notifications
{
    public class MetroNotification : Notification
    {
        private readonly Radar radar;
        private readonly MetroNotificationConfiguration configuration;

        public MetroNotification(Radar radar, MetroNotificationConfiguration configuration)
        {
            Assert.NotNull(radar, "radar");
            Assert.NotNull(configuration, "configuration");

            this.radar = radar;
            this.configuration = configuration;

            if (configuration.DebugInstall)
            {
                DebugInstall();
            }
        }

        public NotificationConfiguration Configuration
        {
            get
            {
                return configuration;
            }
        }

        private void DebugInstall()
        {
            // Toast notifications require that a shortcut on the Start
            // screen with an AppUserModelID.  This is generally created
            // during installation.  For development and debugging, we
            // need to create our own.
            string defaultPath = string.Format(@"{0}\Microsoft\Windows\Start Menu\Programs\{1}.Debug.lnk",
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                Constants.ApplicationName);

            if (File.Exists(defaultPath))
            {
                return;
            }

            string fullPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            using (ShellLink shellLink = new ShellLink())
            {
                shellLink.Path = fullPath;
                shellLink.Arguments = "";
                shellLink.AppUserModelId = Constants.ApplicationId;
                shellLink.Commit();
                shellLink.SaveTo(defaultPath);
            }

            // The toast notification system does not rescan the start menu
            // immediately.
            Thread.Sleep(3000);
        }

        private string GetDefaultSenderImage()
        {
            string filename = Path.Combine(radar.Configuration.ImageCacheDir, "default.gif");

            if (!File.Exists(filename))
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(filename));
                    Resources.Instance.ExtractResourceToFile("PersonPlaceholder", filename);
                }
                catch (Exception)
                {
                    // TODO: log...
                }
            }

            return filename;
        }

        public void Notify(Client client, Event e)
        {
            ToastTemplateType toastTemplate = ToastTemplateType.ToastImageAndText02;
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);

            XmlNodeList toastTextAttributes = toastXml.GetElementsByTagName("text");
            toastTextAttributes[0].InnerText = e.Identity.Name;
            toastTextAttributes[1].InnerText = e.Content;

            XmlNodeList toastImageAttributes = toastXml.GetElementsByTagName("image");

            if (e.Identity.Image != null)
            {
                string localPath = radar.ImageManager.GetImage(e.Identity.Image);
                ((XmlElement)toastImageAttributes[0]).SetAttribute("src", localPath);
            }
            else
            {
                ((XmlElement)toastImageAttributes[0]).SetAttribute("src", GetDefaultSenderImage());
            }

            ((XmlElement)toastImageAttributes[0]).SetAttribute("alt", e.Identity.ToString());

            IXmlNode toastNode = toastXml.SelectSingleNode("/toast");

            XmlElement audio = toastXml.CreateElement("audio");
            audio.SetAttribute("src", "ms-winsoundevent:Notification.IM");

            if (!configuration.Audio)
            {
                audio.SetAttribute("silent", "true");
            }

            toastNode.AppendChild(audio);

            ToastNotification toast = new ToastNotification(toastXml);
            MetroNotificationCallbacks callbacks = new MetroNotificationCallbacks();

            toast.Activated += callbacks.Activated;
            toast.Dismissed += callbacks.Dismissed;
            toast.Failed += callbacks.Failed;

            ToastNotificationManager.CreateToastNotifier(Constants.ApplicationId).Show(toast);
        }

        public void Stop()
        {
        }

        private class MetroNotificationCallbacks
        {
            public void Activated(ToastNotification sender, object args)
            {
            }

            public void Dismissed(ToastNotification sender, ToastDismissedEventArgs args)
            {
            }

            public void Failed(ToastNotification sender, ToastFailedEventArgs args)
            {
                Console.Error.WriteLine("Win8 Toast Notification Failed: {0}", args.ErrorCode.Message);
            }
        }
    }
}