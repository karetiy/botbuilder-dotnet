﻿using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Conversation;
using static Microsoft.Bot.Builder.Conversation.RoutingRules;
using static Microsoft.Bot.Builder.Conversation.Routers;

namespace Microsoft.Bot.Samples
{
    public class Routing
    {
        public static Router BuildHelpRouting()
        {
            var first = First(
                IfTrue(
                    (context) => context.IfIntent("help"),
                    Simple( (context) => context.Reply("No Help for you!"))
                ));

            return first;
        }

        public static Router BuildLoggingRouting()
        {
            var first = First(
                IfTrue(
                    (context) => context.IfIntent("logging"),
                    Simple((context) => EnableOrDisableLogging(context)))
                );

            return first;
        }

        private static void EnableOrDisableLogging(IBotContext context)
        {
            // We know the Logging intent has fired. 
            if (context.Request.Text.Contains("start"))
                ((ConsoleLogger)context.Logger).LoggingEnabled = true;
            else if (context.Request.Text.Contains("stop"))
                ((ConsoleLogger)context.Logger).LoggingEnabled = false;
        }
    }
}
