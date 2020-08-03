using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;

using bankChatBot.Recognizers;

namespace bankChatBot.Dialogs
{
    public class BankDialog: ComponentDialog
    {
        private readonly BankConversationRecognizer _luisRecognizer;
        //private IStatePropertyAccessor<UserProfile> _userProfileAccessor;

        public BankDialog(BankConversationRecognizer luisRecognizer, UserState userState)
            : base(nameof(BankDialog))
        {
            //_userProfileAccessor = userState.CreateProperty<UserProfile>("UserProfile");
            _luisRecognizer = luisRecognizer;

            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                NameStepAsync,
                NameConfirmStepAsync,
                ListChoiseStepAsync,
                ShowChoiseStepAsync,
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_luisRecognizer.IsConfigured)
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("NOTE: LUIS is not configured. To enable all capabilities, add 'LuisAppId', 'LuisAPIKey' and 'LuisAPIHostName' to the appsettings.json file.", inputHint: InputHints.IgnoringInput), cancellationToken);

                return await stepContext.NextAsync(null, cancellationToken);
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private static async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { 
                    Prompt = MessageFactory.Text("Por favor informe seu nome.") 
                }, 
                cancellationToken);
        }

        private async Task<DialogTurnResult> NameConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["name"] = (string)stepContext.Result;

            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { 
                    Prompt = MessageFactory.Text($"Em que posso ajudar {stepContext.Result} ?") 
                }, 
                cancellationToken);
        }

        private static async Task<DialogTurnResult> ListChoiseStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Obrigado {stepContext.Result}."), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Não compreendi o que você quis dizer."), cancellationToken);

            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Por favor, selecione um tópico da lista."),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Cartão de crédito", "Seguro", "Serviços", "Outros" }),
                }, cancellationToken);
        }

        private async Task<DialogTurnResult> ShowChoiseStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"//TODO: Informações sobre '{((FoundChoice)stepContext.Result).Value}'."), cancellationToken);

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }

}