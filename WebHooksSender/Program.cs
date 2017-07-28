// 2 libs required for custom Webhooks
using Microsoft.AspNet.WebHooks;
using Microsoft.AspNet.WebHooks.Diagnostics;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;            // added so we can determine where the tracelog is located 
using System.Reflection;    // added so we can determine where the tracelog is located 

namespace WebHooksSender
{
    /// <summary>
    ///  This is a simple hack to show the use of ASP.NET webhooks. There is ZERO exception handling because this is only a demo. 
    ///  In this example the "subscriber" (Receiver) and "publisher" (Sender) are in the same code.
    ///  These would normally be in two different services but I wanted to keep this simple.
    ///      
    ///  The webhooks store uses in-memory default.  This is obviously not ideal but I want to keep 
    ///  the example simple. (Normally use Microsoft.AspNet.WebHooks.Custom.AzureStorage nuget which 
    ///  can be configured to use Azure storage accounts.)
    ///  </summary>
    class Program
    {
        /// Define the WebHookManager and WebHookStore 
        /// The webhook manager will use the webhook store to locate and fire a webhook for the 
        /// appropriate callback.
        private static IWebHookManager whManager;
        private static IWebHookStore whStore;

        private static string myRequestBin = "";

        static void Main(string[] args)
        {
            Console.WriteLine("Hit ENTER to create a RequestBin for your callback url");
            Console.ReadLine();
            System.Diagnostics.Process.Start("http://requestb.in");

            Console.WriteLine("\nPaste the bin url for your RequestBin below:");
            myRequestBin = Console.ReadLine();

            whStore = new MemoryWebHookStore();

            /// A WebHookManager is used to send a webhook request.  
            /// WebHookManager requires a WebHookStore for tracking subscriptions.
            /// WebHookManager also uses an ILogger-type object as a diagnostics logger. 
            whManager = new WebHookManager(whStore, new TraceLogger());

            Console.WriteLine("\n\nRegistering a Subscriber with WebHookManager");
            registerWebhook();
            Console.WriteLine("\nHit ENTER to fire your webhook");
            Console.ReadLine();

            fireWebhook().Wait();

            Console.WriteLine("\nTracelog is at " + new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName + "\\trace.log");

            Console.WriteLine("Hit ENTER to view the notification in RequestBin");
            Console.ReadLine();
            System.Diagnostics.Process.Start(myRequestBin + "?inspect");
        }

        private static void registerWebhook()
        {
            var webhook = new WebHook();

            Console.WriteLine("\tSubscriber 'user1' will subscribe to 'event1'");

            // Configure the webhook request (subscription)
            webhook.Filters.Add("event1");
            webhook.Properties.Add("FirstName", "John");
            webhook.Properties.Add("LastName", "Evdemon");

            // webhook secret is normally not hard-coded (obviously)
            webhook.Secret = "PSBuMnbzZqVir4OnN4DE10IqB7HXfQ9l2";

            // Identify the callback URL. 
            webhook.WebHookUri = myRequestBin;

            // Register subscription with callback URL to the webhook store
            whStore.InsertWebHookAsync("user1", webhook);

            Console.WriteLine("\tWebhook registered");

        }

        private static async Task fireWebhook()
        {
            // create a simple notification that indicates an event occurred
            // this would normally occur in another service
            var notifications = new List<NotificationDictionary> { new NotificationDictionary("event1", new { Value = "Response from Webhook", Message = "Your Event occurred" }) };

            // simulate a different event and try to fire it for the receiver 
            // the receiver shouldn't get this notification because it didn't register interest in this event
            notifications.Add(new NotificationDictionary("BogusEvent", new { Value = "Response from Webhook", Message = "Your bogus event occurred" }));

            Console.WriteLine("Firing webhook (normally done by an external service)...");

            // fire the webhook with multiple notifications (invoke the callback URL)
            var x = await whManager.NotifyAsync("user1", notifications);

        }
    }
}