﻿

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.13.1

using EatCleanBot.Conversation;
using EatCleanBot.Models;
using EatCleanBot.SaveInfor;
using EatCleanBot.SentimentPredict;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.Number;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EatCleanBot.Bots
{
    public class EchoBot : ActivityHandler
    {
        VegafoodContext context;
        public VegafoodContext Context { get { return context; } }

        private readonly BotState _userState;

        private readonly BotState _conversationState;

        protected readonly int ExpireAfterSeconds;
        protected readonly IStatePropertyAccessor<DateTime> LastAccessedTimeProperty;

        public EchoBot(UserState userState, ConversationState conversationState)
        {
            _conversationState = conversationState;
            _userState = userState;
            context = new VegafoodContext();

        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {

            var welcomeText = "Hello and welcome! What's your name ?";
            await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);

        }
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // get state property
            var UserAccessor = _userState.CreateProperty<UserProfile>("UserFrofile");
            // gan cai state
            var Profile = await UserAccessor.GetAsync(turnContext, () => new UserProfile(), cancellationToken);

            var ConversationAccessor = _conversationState.CreateProperty<ConversationFlow>("ConversationFlow");
            var flow = await ConversationAccessor.GetAsync(turnContext, () => new ConversationFlow(), cancellationToken);
            await HandlerTurn(flow, Profile, turnContext, cancellationToken);
            await _userState.SaveChangesAsync(turnContext, false, cancellationToken);

            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);

        }

        // method dùng để lưu thông tin sentiment of user into DB
        public async Task TakeSentiment(CustomerSentiment userSentiment)
        {
            var x = userSentiment.VegaComment;
            var ten = userSentiment.UserName;
            context.CustomerSentiments.Add(userSentiment);
            await context.SaveChangesAsync();
        }

        public async Task HandlerTurn(ConversationFlow flow, UserProfile profile, ITurnContext turnContext, CancellationToken cancellationToken)
        {

            var input = turnContext.Activity.Text?.Trim();
            if (input == "Nutrition Consulting")
                flow.UserChoose = ConversationFlow.Choose.Consulting;
            if (input == "Vegafood's Product")
                flow.UserChoose = ConversationFlow.Choose.Product;
            string message;
            var cal = ConversationFlow.BMI.Weight;


            switch (flow.LastQuestionAsked)
            {
                case ConversationFlow.Question.Name:
                    if (ValidateName(input, out var name, out message))
                    {
                        // property Name of profile state is name that uset typed 
                        profile.Name = name;

                        // use static class to save info, because it had not deleted when run class many times
                        Sentiment.UserName = name;

                        await turnContext.SendActivityAsync($"Hi {profile.Name}.", null, null, cancellationToken);
                        SendSuggestedActionsAsync(turnContext, cancellationToken);

                        flow.LastQuestionAsked = ConversationFlow.Question.Choose;

                        break;
                    }
                    else
                    {
                        // if validation return is false 
                        await turnContext.SendActivityAsync(message ?? "I'm sorry, I didn't understand that.", null, null, cancellationToken);
                        break;
                    }
                    break;
                case ConversationFlow.Question.Choose:
                    if (flow.UserChoose == ConversationFlow.Choose.Consulting)
                    {
                        if (flow.Calculation == ConversationFlow.BMI.None)
                        {
                            await turnContext.SendActivityAsync($"Hey {profile.Name}, How many kilos do you weigh?", null, null, cancellationToken);
                            flow.Calculation = ConversationFlow.BMI.Weight;

                        }
                        else if (flow.Calculation == ConversationFlow.BMI.Weight)
                        {

                            if (ValidateWeight(input, out var weight, out message))
                            {
                                profile.Weight = weight;
                                await turnContext.SendActivityAsync($"I have your weight as {profile.Weight} kg.", null, null, cancellationToken);
                                await turnContext.SendActivityAsync("How tall are you?(met)", null, null, cancellationToken);
                                flow.Calculation = ConversationFlow.BMI.Height;
                                break;
                            }
                            else
                            {
                                await turnContext.SendActivityAsync(message ?? "I'm sorry, I didn't understand that.", null, null, cancellationToken);
                                break;
                            }
                        }
                        else
                        {
                            if (ValidateHeight(input, out var height, out message))
                            {
                                profile.Height = height;
                                double x = (profile.Weight) / (profile.Height * profile.Height);
                                double BMI = Math.Round(x, 2);
                                await turnContext.SendActivityAsync($"Your BMI is: {BMI}");


                                flow.UserChoose = ConversationFlow.Choose.Product;

                                if (flow.UserEmotion == ConversationFlow.Emotion.End)
                                {
                                    NoticeBMI(BMI, turnContext, cancellationToken);
                                    await turnContext.SendActivityAsync($"Thanks for completing the support for Vegafood.We wil support you as soon as possible.");
                                }
                                else
                                {
                                    NoticeBMI(BMI, turnContext, cancellationToken);
                                    await turnContext.SendActivityAsync($"How do you feel about Vegafood ???");
                                    flow.UserEmotion = ConversationFlow.Emotion.Service;
                                }
                                break;
                            }
                            else
                            {
                                await turnContext.SendActivityAsync(message ?? "I'm sorry, I didn't understand that.", null, null, cancellationToken);
                                break;
                            }
                        }
                        break;
                    }
                    if (flow.UserChoose == ConversationFlow.Choose.Product)
                    {
                        if (flow.UserEmotion == ConversationFlow.Emotion.None)
                        {
                            await turnContext.SendActivityAsync($"How do you feel about Vegafood ???");
                            flow.UserEmotion = ConversationFlow.Emotion.Service;

                        }
                        else if (flow.UserEmotion == ConversationFlow.Emotion.Service)
                        {
                            var predict = new PredictSentiment(input);
                            string result = predict.Predict();
                            Sentiment.VegaPredict = result;
                            Sentiment.VegaComment = input;
                            await turnContext.SendActivityAsync($"Your sentiment is: {result}");
                            await turnContext.SendActivityAsync($"How do you feel about Vegafood's service ?");
                            flow.UserEmotion = ConversationFlow.Emotion.Product;
                        }
                        else if (flow.UserEmotion == ConversationFlow.Emotion.Product)
                        {
                            var predict = new PredictSentiment(input);
                            string result = predict.Predict();
                            Sentiment.ServicePredict = result;
                            Sentiment.ServiceComment = input;
                            await turnContext.SendActivityAsync($"Your sentiment about our service is: {result}");
                            await turnContext.SendActivityAsync($"How do you feel about food of Vegafood ?");
                            flow.UserEmotion = ConversationFlow.Emotion.End;

                        }
                        else if (flow.UserEmotion == ConversationFlow.Emotion.End)
                        {
                            if (Sentiment.End)
                            {
                                var predict = new PredictSentiment(input);
                                string result = predict.Predict();
                                Sentiment.FoodPredict = result;
                                Sentiment.FoodComment = input;
                                await turnContext.SendActivityAsync($"Your sentiment about our food is: {result}");
                                await turnContext.SendActivityAsync($"Thanks for your respond about Vegafood !!!.");
                                if (Sentiment.Check)
                                {
                                    CustomerSentiment cusen = new CustomerSentiment();
                                    cusen.UserName = Sentiment.UserName;
                                    cusen.Time = Sentiment.Time;
                                    cusen.VegaPredict = Sentiment.VegaPredict;
                                    cusen.VegaComment = Sentiment.VegaComment;
                                    cusen.FoodComment = Sentiment.FoodComment;
                                    cusen.FoodPredict = Sentiment.FoodPredict;
                                    cusen.ServiceComment = Sentiment.ServiceComment;
                                    cusen.ServicePredict = Sentiment.ServicePredict;
                                    TakeSentiment(cusen);
                                    Sentiment.Check = false;

                                }

                                // nếu đã thực hiện tư vấn dinh dưỡng rồi
                                if (flow.Calculation == ConversationFlow.BMI.Height)
                                {
                                    await turnContext.SendActivityAsync($"Thanks for completing the support         for Vegafood.We wil support you as soon as possible. ");

                                }
                                else
                                {
                                    flow.UserChoose = ConversationFlow.Choose.Consulting;

                                }
                                Sentiment.End = false;
                            }
                            else
                            {
                                await turnContext.SendActivityAsync($"This is Vegafood, We are very happy to receive your feedback.");
                            }

                        }
                        else
                            await turnContext.SendActivityAsync($"Thank you very much.");

                    }
                    break;

            }
        }

        private async Task NoticeBMI(double BMI, ITurnContext turnContext, CancellationToken cancellationToken)
        {
            if (BMI < 18.5)
                await turnContext.SendActivityAsync($"Your BMI index < 18.5.You should review your diet, or eat more diligently. ", null, null, cancellationToken);
            else if (BMI > 18.5 && BMI < 24.9)
                await turnContext.SendActivityAsync($"Your BMI index from 18.5 to 24. Please continue to maintain this BMI index. It's good for your healthy. ", null, null, cancellationToken);
            else if (BMI > 25 && BMI < 29.9)
                await turnContext.SendActivityAsync($"Your BMI index from 25 to 29.9. You are overweight, eat less and exercise more. ", null, null, cancellationToken);
            else
                await turnContext.SendActivityAsync($"Your BMI index greater than 30. You are obese, your body is very overweight. You should reduce your eating and work hard and exercise more.Fighting!!! ", null, null, cancellationToken);

        }
        private bool ValidateName(string input, out string name, out string message)
        {
            name = null;
            message = null;

            if (string.IsNullOrWhiteSpace(input))
            {
                message = "Please enter a name that contains at least one character.";
            }
            else
            {
                name = input.Trim();
            }

            return message is null;
        }

        private async Task SendSuggestedActionsAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var reply = MessageFactory.Text("What can i help you ?");

            // Type = ActionTypes.ImBack,Image = "https://via.placeholder.com/20/FF0000?text=R", ImageAltText = "R" 
            reply.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                    {

                        new CardAction() { Title = "Nutrition Consulting",Type = ActionTypes.ImBack, Value = "Nutrition Consulting", },
                        new CardAction() { Title = "Vegafood's Product", Type = ActionTypes.ImBack, Value = "Vegafood's Product", Image = "https://via.placeholder.com/20/FFFF00?text=Y", ImageAltText = "Y" },
                        //new CardAction() { Title = "Blue", Type = ActionTypes.ImBack, Value = "Blue", Image = "https://via.placeholder.com/20/0000FF?text=B", ImageAltText = "B"   },
                    },
            };
            await turnContext.SendActivityAsync(reply, cancellationToken);

        }
        private bool ValidateWeight(string input, out double weight, out string message)
        {
            weight = 0;
            message = null;

            // Try to recognize the input as a number. This works for responses such as "twelve" as well as "12".
            try
            {
                // Attempt to convert the Recognizer result to an integer. This works for "a dozen", "twelve", "12", and so on.
                // The recognizer returns a list of potential recognition results, if any.

                var results = NumberRecognizer.RecognizeNumber(input, Culture.English);

                foreach (var result in results)
                {
                    // The result resolution is a dictionary, where the "value" entry contains the processed string.
                    if (result.Resolution.TryGetValue("value", out var value))
                    {
                        string Va = value.ToString();
                        weight = double.Parse(Va);
                        //weight = (double)x;
                        //var a = weight;
                        if (weight > 0)
                        {
                            return true;
                        }
                    }
                }

                message = "Please enter an weight greater than 0. ";
            }
            catch
            {
                message = "I'm sorry, I could not interpret that as an age. Please enter an age between 18 and 120.";
            }

            return message is null;
        }
        private bool ValidateHeight(string input, out double height, out string message)
        {
            height = 1;
            message = null;

            // Try to recognize the input as a date-time. This works for responses such as "11/14/2018", "9pm", "tomorrow", "Sunday at 5pm", and so on.
            // The recognizer returns a list of potential recognition results, if any.
            try
            {
                // var results = DateTimeRecognizer.RecognizeDateTime(input, Culture.English);

                // Check whether any of the recognized date-times are appropriate,
                // and if so, return the first appropriate date-time. We're checking for a value at least an hour in the future.
                //var earliest = DateTime.Now.AddHours(1.0);

                var results = NumberRecognizer.RecognizeNumber(input, Culture.English);

                foreach (var result in results)
                {
                    // The result resolution is a dictionary, where the "value" entry contains the processed string.
                    if (result.Resolution.TryGetValue("value", out var value))
                    {
                        string Va = value.ToString();
                        height = double.Parse(Va);
                        //weight = (double)x;
                        //var a = weight;
                        if (height > 0)
                        {
                            return true;
                        }
                    }
                }

                message = "Please enter an height greater than 0. ";


            }
            catch
            {
                message = "I'm sorry, I could not interpret that as an height. Please enter a height  greater than 0.";
            }

            return false;
        }
    }
}