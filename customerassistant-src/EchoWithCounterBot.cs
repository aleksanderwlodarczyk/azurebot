// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Bot.Builder.Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents;
using Microsoft.EntityFrameworkCore.Extensions.Internal;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using EchoBotWithCounter;

namespace Microsoft.BotBuilderSamples
{
    /// <summary>
    /// Represents a bot that processes incoming activities.
    /// For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    /// This is a Transient lifetime service.  Transient lifetime services are created
    /// each time they're requested. For each Activity received, a new instance of this
    /// class is created. Objects that are expensive to construct, or have a lifetime
    /// beyond the single turn, should be carefully managed.
    /// For example, the <see cref="MemoryStorage"/> object and associated
    /// <see cref="IStatePropertyAccessor{T}"/> object are created with a singleton lifetime.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
    public class EchoWithCounterBot : IBot
    {
        private readonly EchoBotAccessors _accessors;
        private readonly ILogger _logger;

        private const string CosmosServiceEndpoint = "https://sq7ofg.documents.azure.com:443/";
        private const string CosmosDBKey = "Ke2a1l6rHp6ZmxMMiDsdfHhzk6lql3Rh41cygbtYRdOqGfvBq1QnIoFXNPlVdGv4NoZ6qLFG2sG6yyTZYLbelQ==";
        private const string CosmosDBDatabaseName = "bot-cosmos-sql-db";
        private const string CosmosDBCollectionName = "bot-storage";

        private static readonly AzureBlobStorage _myStorage = new AzureBlobStorage("DefaultEndpointsProtocol=https;AccountName=sq7ofgblobs;AccountKey=2k0LVUrttdcPB50R3DNIgZrB2ff1yapC6Iv0r/SeCKFW4D+bL2ezwy8zO0lCw/Adw2ysPSdsu13OZ07RtXufFw==;EndpointSuffix=core.windows.net", "bot-storage");


        private readonly AzureBlobTranscriptStore _transcriptStore = new AzureBlobTranscriptStore("DefaultEndpointsProtocol=https;AccountName=sq7ofgblobs;AccountKey=2k0LVUrttdcPB50R3DNIgZrB2ff1yapC6Iv0r/SeCKFW4D+bL2ezwy8zO0lCw/Adw2ysPSdsu13OZ07RtXufFw==;EndpointSuffix=core.windows.net", "bot-storage");

        /// <param name="transcriptStore">Injected via ASP.NET dependency injection.</param>
        public EchoWithCounterBot(AzureBlobTranscriptStore transcriptStore)
        {
            _transcriptStore = transcriptStore ?? throw new ArgumentNullException(nameof(transcriptStore));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EchoWithCounterBot"/> class.
        /// </summary>
        /// <param name="accessors">A class containing <see cref="IStatePropertyAccessor{T}"/> used to manage state.</param>
        /// <param name="loggerFactory">A <see cref="ILoggerFactory"/> that is hooked to the Azure App Service provider.</param>
        /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-2.1#windows-eventlog-provider"/>
        public EchoWithCounterBot(EchoBotAccessors accessors, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<EchoWithCounterBot>();
            _logger.LogTrace("EchoBot turn start.");
            _accessors = accessors ?? throw new System.ArgumentNullException(nameof(accessors));
        }



        /// <summary>
        /// Every conversation turn for our Echo Bot will call this method.
        /// There are no dialogs used, since it's "single turn" processing, meaning a single
        /// request and response.
        /// </summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn. </param>
        /// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        /// <seealso cref="BotStateSet"/>
        /// <seealso cref="ConversationState"/>
        /// <seealso cref="IMiddleware"/>

        private string userEmail;
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Handle Message activity type, which is the main activity type for shown within a conversational interface
            // Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
            // see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {

                await _myStorage.ReadAsync(new string[] { " " });

                Console.WriteLine("Do i send request?");
                // IT WORKS !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                //var testDic = new Dictionary<string, object>();
                //testDic.Add("test", "19");
                //_myStorage.WriteAsync(testDic);

                var activity = turnContext.Activity;
                // Get the conversation state from the turn context.
                var oldState = await _accessors.CounterState.GetAsync(turnContext, () => new CounterState());

                // Bump the turn count for this conversation.
                var newState = new CounterState { TurnCount = oldState.TurnCount + 1 };

                // Set the property using the accessor.
                await _accessors.CounterState.SetAsync(turnContext, newState);

                // Save the new turn count into the conversation state.
                await _accessors.ConversationState.SaveChangesAsync(turnContext);
                if (State.UserPromptedForName)
                {
                    string[] words = turnContext.Activity.Text.Split(" ");
                    string name = words[words.Length - 1];
                    State.User.name = name;
                    State.UserPromptedForName = false;
                    State.UserReadyToSave = true;
                }
                if (State.UserPromptedForEmail)
                {
                    if (activity.Text.Contains("@"))
                    {
                        string[] words = activity.Text.Split(" ");
                        foreach(string word in words)
                        {
                            if (word.Contains("@"))
                            {
                                userEmail = word;
                                State.GotEmail = true;
                                break;
                            }
                        }
                    }
                    State.UserPromptedForEmail = false;
                }

                if (activity.Text == "!history")
                {
                    // Download the activities from the Transcript (blob store) when a request to upload history arrives.
                    var connectorClient = turnContext.TurnState.Get<ConnectorClient>(typeof(IConnectorClient).FullName);
                    // Get all the message type activities from the Transcript.
                    string continuationToken = null;
                    var count = 0;
                    do
                    {
                        var pagedTranscript = await _transcriptStore.GetTranscriptActivitiesAsync(activity.ChannelId, activity.Conversation.Id);
                        var activities = pagedTranscript.Items
                           .Where(a => a.Type == ActivityTypes.Message)
                           .Select(ia => (Activity)ia)
                           .ToList();

                        var transcript = new Transcript(activities);

                        await connectorClient.Conversations.SendConversationHistoryAsync(activity.Conversation.Id, transcript, cancellationToken: cancellationToken);

                        continuationToken = pagedTranscript.ContinuationToken;
                    }
                    while (continuationToken != null);

                    List<string> storedTranscripts = new List<string>();
                    PagedResult<TranscriptInfo> pagedResult = null;
                    var pageSize = 0;
                    do
                    {
                        pagedResult = await _transcriptStore.ListTranscriptsAsync("emulator", pagedResult?.ContinuationToken);

                        // transcript item contains ChannelId, Created, Id.
                        // save the converasationIds (Id) found by "ListTranscriptsAsync" to a local list.
                        foreach (var item in pagedResult.Items)
                        {
                            // Make sure we store an unescaped conversationId string.
                            var strConversationId = item.Id;
                            storedTranscripts.Add(Uri.UnescapeDataString(strConversationId));
                        }
                    } while (pagedResult.ContinuationToken != null);

                    var numTranscripts = storedTranscripts.Count();
                    for (int i = 0; i < numTranscripts; i++)
                    {
                        PagedResult<IActivity> pagedActivities = null;
                        do
                        {
                            string thisConversationId = storedTranscripts[i];
                            // Find all inputs in the last 24 hours.
                            DateTime yesterday = DateTime.Now.AddDays(-1);
                            // Retrieve iActivities for this transcript.
                            var story = "";
                            pagedActivities = await _transcriptStore.GetTranscriptActivitiesAsync("emulator", thisConversationId, pagedActivities?.ContinuationToken, yesterday);
                            foreach (var item in pagedActivities.Items)
                            {
                                // View as message and find value for key "text" :
                                var thisMessage = item.AsMessageActivity();
                                    var userInput = thisMessage.Text;
                                story += userInput;
                            }
                            await turnContext.SendActivityAsync(story);
                        } while (pagedActivities.ContinuationToken != null);

                        for (int j = 0; j < numTranscripts; j++)
                        {
                            // Remove all stored transcripts except the last one found.
                            if (i > 0)
                            {
                                string thisConversationId = storedTranscripts[i];

                                await turnContext.SendActivityAsync(storedTranscripts[i]);
                                await _transcriptStore.DeleteTranscriptAsync("emulator", thisConversationId);
                            }
                        }
                    }
                    // Save new list to your Storage.

                    // Echo back to the user whatever they typed.

                }
                else
                {
                    var responseMessage = "";
                    if (State.User == null || !State.Registered)
                    {
                        if (!State.GotEmail)
                        {
                            responseMessage = "Hello!\nI'd like to know who you are. Could you give me your e-mail address, please?";
                            await turnContext.SendActivityAsync($"{responseMessage}");
                            State.UserPromptedForEmail = true;
                        }
                        else
                        {
                            string[] usersBlobNames = { " ", "/" };
                            var users = new List<UserData>();
                            var usersFromDb = await _myStorage.ReadAsync(usersBlobNames).ConfigureAwait(true);
                            //var usersFromDb2 = new Dictionary<string, object>();

                            foreach (object u in usersFromDb)
                            {
                                UserData dbUser = (UserData)u;
                                users.Add(dbUser);
                                if(dbUser.emailnormalized == userEmail.ToUpper())
                                {
                                    State.User = dbUser;
                                    State.Registered = true;
                                }
                            }
                            if(State.User == null && !State.UserPromptedForName)
                            {
                                State.User = new UserData(userEmail);
                                responseMessage = "Great!, What's your name?";
                                State.UserPromptedForName = true;
                                await turnContext.SendActivityAsync(responseMessage);
                            }

                            if (State.UserReadyToSave) { 
                                users.Add(State.User);
                                State.UserReadyToSave = false;
                                // Update Users in DB
                                await _myStorage.DeleteAsync(usersBlobNames);
                                var usersDic = new Dictionary<string, object>();
                                foreach (UserData u in users)
                                {
                                    usersDic.Add(u.emailnormalized, u);
                                }
                                await _myStorage.WriteAsync(usersDic);
                                State.Registered = true;
                                responseMessage = $"Hello, {State.User.name}!";
                                await turnContext.SendActivityAsync($"{responseMessage}");
                            }

                        }
                    }
                    else
                    {
                        responseMessage = $"Hello, {State.User.name}!";
                        await turnContext.SendActivityAsync($"{responseMessage}");
                    }
                    
                    responseMessage = $"Turn {newState.TurnCount}: You sent '{turnContext.Activity.Text}'\n";
                    await turnContext.SendActivityAsync($"{responseMessage}");



                }
            }
        }
    }
}