// 2 libs required for Webhooks
using Microsoft.AspNet.WebHooks;
using Microsoft.AspNet.WebHooks.Diagnostics;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebHooksSender
{
    /// <summary>
    ///  This is a simple hack to show the use of ASP.NET webhooks.
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
        
        /// Variable below is the URL for the callback. 
        /// Go to request bin (http://requestb.in) and update the url below.
        private static string myRequestBin = "http://requestb.in/1hkv2g11";
        
        static void Main(string[] args)
        {
            whStore = new MemoryWebHookStore();

            /// A WebHookManager is used to send a webhook request.  
            /// WebHookManager requires a WebHookStore for tracking subscriptions.
            /// WebHookManager also uses an ILogger-type object as a diagnostics logger. 
            whManager = new WebHookManager(whStore, new TraceLogger());

            Console.WriteLine("Registering a Subscriber with WebHookManager");
            registerWebhook();
            Console.WriteLine("\nGo check out your RequestBin at " + myRequestBin + "\n\nHit ENTER to fire your webhook");
            Console.ReadLine();

            Console.WriteLine("Triggering an event to fire the webhook (normally done by an external service)");
            fireWebhook().Wait();

            Console.WriteLine("\nGo refresh your RequestBin page at " + myRequestBin + "\n\nHit ENTER to exit");
            Console.ReadLine();
        }

        private static void registerWebhook()
        {
            var webhook = new WebHook();

            Console.WriteLine("Subscriber 'user1' will subscribe to 'event1'");

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
        }

        private static async Task fireWebhook()
        {
            // create a simple notification that indicates an event occurred
            // this would normally occur in another service
            var notifications = new List<NotificationDictionary> { new NotificationDictionary("event1", new { Value = "Response from Webhook", Message = "Your Event occurred"}) };

            // simulate a different event and try to fire it for the receiver 
            // the receiver shouldn't get this notification because it didn't register interest in this event
            notifications.Add(new NotificationDictionary("BogusEvent", new { Value = "Response from Webhook", Message = "Your bogus event occurred" }));
            
            // fire the webhook with multiple notifications (invoke the callback URL)
            var x = await whManager.NotifyAsync("user1", notifications);

        }
    }
}