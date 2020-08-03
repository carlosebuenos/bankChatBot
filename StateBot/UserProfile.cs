// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Schema;

namespace bankChatBot.StateBot
{
    // Defines a state property used to track information about the user.
    public class UserProfile
    {
        public string Transport { get; set; }

        public string Name { get; set; }

        public int Age { get; set; }

        public string Date { get; set; }

        public Attachment Picture { get; set; }
    }
}
