using DesktopToastsSample.ShellHelpers;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using MS.WindowsAPICodePack.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;


//Code from https://code.msdn.microsoft.com/windowsdesktop/Sending-toast-notifications-71e230a2

namespace VideoTrimmer
{
    class Toasts
    {
        private const String APP_ID = "xTdub.VideoTrimmer";

        /// <summary>
        /// The file save location
        /// </summary>
        public string butter;

        // In order to display toasts, a desktop application must have a shortcut on the Start menu.
        // Also, an AppUserModelID must be set on that shortcut.
        // The shortcut should be created as part of the installer. The following code shows how to create
        // a shortcut and assign an AppUserModelID using Windows APIs. You must download and include the 
        // Windows API Code Pack for Microsoft .NET Framework for this code to function
        //
        // Included in this project is a wxs file that be used with the WiX toolkit
        // to make an installer that creates the necessary shortcut. One or the other should be used.
        public bool TryCreateShortcut()
        {
            String shortcutPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Microsoft\\Windows\\Start Menu\\Programs\\Video2GIF.lnk";
            if (!File.Exists(shortcutPath))
            {
                InstallShortcut(shortcutPath);
                return true;
            }
            return false;
        }

        private void InstallShortcut(String shortcutPath)
        {
            // Find the path to the current executable
            String exePath = Process.GetCurrentProcess().MainModule.FileName;
            IShellLinkW newShortcut = (IShellLinkW)new CShellLink();

            // Create a shortcut to the exe
            DesktopToastsSample.ShellHelpers.ErrorHelper.VerifySucceeded(newShortcut.SetPath(exePath));
            DesktopToastsSample.ShellHelpers.ErrorHelper.VerifySucceeded(newShortcut.SetArguments(""));

            // Open the shortcut property store, set the AppUserModelId property
            IPropertyStore newShortcutProperties = (IPropertyStore)newShortcut;

            using (PropVariant appId = new PropVariant(APP_ID))
            {
                DesktopToastsSample.ShellHelpers.ErrorHelper.VerifySucceeded(newShortcutProperties.SetValue(SystemProperties.System.AppUserModel.ID, appId));
                DesktopToastsSample.ShellHelpers.ErrorHelper.VerifySucceeded(newShortcutProperties.Commit());
            }

            // Commit the shortcut to disk
            IPersistFile newShortcutSave = (IPersistFile)newShortcut;

            DesktopToastsSample.ShellHelpers.ErrorHelper.VerifySucceeded(newShortcutSave.Save(shortcutPath, true));
        }

        // Create and show the toast.
        // See the "Toasts" sample for more detail on what can be done with toasts
        public void ShowToast(string text)
        {

            // Get a toast XML template
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);

            // Fill in the text elements
            XmlNodeList stringElements = toastXml.GetElementsByTagName("text");
            stringElements[0].AppendChild(toastXml.CreateTextNode("Video2GIF"));
            stringElements[1].AppendChild(toastXml.CreateTextNode(text));

            // Specify the absolute path to an image
            //String imagePath = "file:///" + Path.GetFullPath("toastImageAndText.png");
            //XmlNodeList imageElements = toastXml.GetElementsByTagName("image");
            //imageElements[0].Attributes.GetNamedItem("src").NodeValue = imagePath;

            // Create the toast and attach event listeners
            ToastNotification toast = new ToastNotification(toastXml);
            toast.Activated += ToastActivated;
            toast.Dismissed += ToastDismissed;
            toast.Failed += ToastFailed;

            // Show the toast. Be sure to specify the AppUserModelId on your application's shortcut!
            ToastNotificationManager.CreateToastNotifier(APP_ID).Show(toast);
        }

        private void ToastActivated(ToastNotification sender, object e)
        {
            Process.Start(butter);
            //Dispatcher.Invoke(() =>
            //{
            //    e.GetType();
            //    //Activate();
            //    //Output.Text = "The user activated the toast.";
            //});
        }

        private void ToastDismissed(ToastNotification sender, ToastDismissedEventArgs e)
        {
            //String outputText = "";
            //switch (e.Reason)
            //{
            //    case ToastDismissalReason.ApplicationHidden:
            //        outputText = "The app hid the toast using ToastNotifier.Hide";
            //        break;
            //    case ToastDismissalReason.UserCanceled:
            //        outputText = "The user dismissed the toast";
            //        break;
            //    case ToastDismissalReason.TimedOut:
            //        outputText = "The toast has timed out";
            //        break;
            //}

            //Dispatcher.Invoke(() =>
            //{
            //    Output.Text = outputText;
            //});
        }

        private void ToastFailed(ToastNotification sender, ToastFailedEventArgs e)
        {
        //    Dispatcher.Invoke(() =>
        //    {
        //        sender.Content.GetHashCode();
        //        //Output.Text = "The toast encountered an error.";
        //    });
        }
    }
}
