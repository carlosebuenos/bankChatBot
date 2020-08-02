// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

using bankChatBot.StateBot;

namespace bankChatBot.Bots
{
    public class StateManagementBot : ActivityHandler
    {
        private BotState _conversationState;
        private BotState _userState;

        private MemoryStorage _storage;

        public StateManagementBot(ConversationState conversationState, UserState userState, MemoryStorage storage)
        {
            _conversationState = conversationState;
            _userState = userState;
            _storage = storage;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occurred during the turn.
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync("Olá, bem vindo. (StateManagement)");
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // Get the state properties from the turn context.

            var conversationStateAccessors = _conversationState.CreateProperty<ConversationData>(nameof(ConversationData));
            var conversationData = await conversationStateAccessors.GetAsync(turnContext, () => new ConversationData());

            var userStateAccessors = _userState.CreateProperty<UserProfile>(nameof(UserProfile));
            var userProfile = await userStateAccessors.GetAsync(turnContext, () => new UserProfile());

            if (string.IsNullOrEmpty(userProfile.Name))
            {
                // First time around this is set to false, so we will prompt user for name.
                if (conversationData.PromptedUserForName)
                {
                    // Set the name to what the user provided.
                    userProfile.Name = turnContext.Activity.Text?.Trim();

                    // Acknowledge that we got their name.
                    await turnContext.SendActivityAsync($"Obrigado {userProfile.Name}. Para ver os dados da conversa digita algo.");

                    // Reset the flag to allow the bot to go through the cycle again.
                    conversationData.PromptedUserForName = false;
                }
                else
                {
                    // Prompt the user for their name.
                    await turnContext.SendActivityAsync($"Qual o seu nome?");

                    // Set the flag to true, so we don't prompt in the next turn.
                    conversationData.PromptedUserForName = true;
                }
            }
            else
            {
                // Add message details to the conversation data.
                // Convert saved Timestamp to local DateTimeOffset, then to string for display.
                var messageTimeOffset = (DateTimeOffset) turnContext.Activity.Timestamp;
                var localMessageTime = messageTimeOffset.ToLocalTime();
                conversationData.Timestamp = localMessageTime.ToString();
                conversationData.ChannelId = turnContext.Activity.ChannelId.ToString();

                // Display state data.
                await turnContext.SendActivityAsync($"{userProfile.Name} enviou: {turnContext.Activity.Text}");
                await turnContext.SendActivityAsync($"Recebido em: {conversationData.Timestamp}");
                await turnContext.SendActivityAsync($"Origem da mensagem: {conversationData.ChannelId}");
            }


            // preserve user input.
            var utterance = turnContext.Activity.Text;
            // make empty local logitems list.
            UtteranceLog logItems = null;

            // see if there are previous messages saved in storage.
            try
            {
                string[] utteranceList = { "UtteranceLog" };
                logItems = _storage.ReadAsync<UtteranceLog>(utteranceList).Result?.FirstOrDefault().Value;
            }
            catch
            {
                // Inform the user an error occured.
                await turnContext.SendActivityAsync("Desculpe, algo de errado ocorreu ao ler as mensagens armazenadas!");
            }

            // If no stored messages were found, create and store a new entry.
            if (logItems is null)
            {
                // add the current utterance to a new object.
                logItems = new UtteranceLog();
                logItems.UtteranceList.Add(utterance);
                // set initial turn counter to 1.
                logItems.TurnNumber++;

                // Show user new user message.
                await turnContext.SendActivityAsync($"{logItems.TurnNumber}: A lista agora é: {string.Join(", ", logItems.UtteranceList)}");

                // Create Dictionary object to hold received user messages.
                var changes = new Dictionary<string, object>();
                {
                    changes.Add("UtteranceLog", logItems);
                }
                try
                {
                    // Save the user message to your Storage.
                    await _storage.WriteAsync(changes, cancellationToken);
                }
                catch
                {
                    // Inform the user an error occured.
                    await turnContext.SendActivityAsync("Desculpe, algo de errado ocorreu ao armazenar as mensagens!");
                }
            }
            // Else, our Storage already contained saved user messages, add new one to the list.
            else
            {
                // add new message to list of messages to display.
                logItems.UtteranceList.Add(utterance);
                // increment turn counter.
                logItems.TurnNumber++;

                // show user new list of saved messages.
                await turnContext.SendActivityAsync($"{logItems.TurnNumber}: A lista agora é: {string.Join(", ", logItems.UtteranceList)}");

                // Create Dictionary object to hold new list of messages.
                var changes = new Dictionary<string, object>();
                {
                    changes.Add("UtteranceLog", logItems);
                };

                try
                {
                    // Save new list to your Storage.
                    await _storage.WriteAsync(changes,cancellationToken);
                }
                catch
                {
                    // Inform the user an error occured.
                    await turnContext.SendActivityAsync("Desculpe, algo de errado ocorreu ao armazenar as mensagens!");
                }
            }
        }
    }
}

